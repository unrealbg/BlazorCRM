using Crm.Web.Components;
using Crm.Application.Services;
using Crm.Infrastructure.Services;
using Crm.Infrastructure.Persistence;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Crm.Infrastructure.Identity;
using Microsoft.AspNetCore.Authorization;
using Crm.Infrastructure.Seeding;
using Crm.Application;
using Crm.Application.Common.Multitenancy;
using Crm.Infrastructure.Multitenancy;
using Crm.Application.Security;
using Serilog;
using Quartz;
using Crm.Infrastructure.Jobs;
using Crm.Application.Files;
using Crm.Infrastructure.Files;
using Crm.Application.Common.Abstractions;
using Crm.Application.Notifications;
using Crm.Infrastructure.Notifications;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authentication;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using System.Security.Claims;
using Crm.Infrastructure.Security;
using Crm.Contracts.Auth;
using Crm.Contracts.Search;
using MediatR;
using Crm.Application.Companies;
using Crm.Application.Contacts;
using Crm.Application.Deals;
using Crm.Application.Activities;
using Crm.Application.Tasks;
using Asp.Versioning;

using Microsoft.AspNetCore.RateLimiting;
using Microsoft.AspNetCore.DataProtection;

using OpenTelemetry.Trace;
using OpenTelemetry.Metrics;
using Crm.Web.Infrastructure;

using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.ResponseCompression;
using Microsoft.AspNetCore.OutputCaching;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.Extensions.Caching.Memory;
using Npgsql;

var builder = WebApplication.CreateBuilder(args);

// Serilog
builder.Host.UseSerilog((ctx, lc) =>
{
    lc.ReadFrom.Configuration(ctx.Configuration);
});

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

// Antiforgery (used by HTML form endpoints)
builder.Services.AddAntiforgery();

// ProblemDetails for consistent API errors
builder.Services.AddProblemDetails();

// API Versioning
builder.Services.AddApiVersioning(o =>
{
    o.DefaultApiVersion = new ApiVersion(1, 0);
    o.AssumeDefaultVersionWhenUnspecified = true;
    o.ReportApiVersions = true;
});

// Provide AuthenticationState for AuthorizeView/AuthorizeRouteView
builder.Services.AddCascadingAuthenticationState();

// Application layer
builder.Services.AddApplication();
builder.Services.AddScoped(typeof(IPipelineBehavior<,>), typeof(PermissionBehavior<,>));
builder.Services.AddScoped<IPermissionEvaluator, PermissionEvaluator>();

// Tenant provider
builder.Services.AddHttpContextAccessor();
builder.Services.Configure<TenantOptions>(builder.Configuration.GetSection("Tenancy"));
builder.Services.Configure<AttachmentStorageOptions>(builder.Configuration.GetSection("Attachments"));
builder.Services.AddScoped<ITenantResolver, SubdomainTenantResolver>();
builder.Services.AddScoped<ITenantContextAccessor, TenantContextAccessor>();
builder.Services.AddScoped<ITenantProvider, HttpTenantProvider>();
builder.Services.AddScoped<IUserTenantMembership, IdentityUserTenantMembership>();
builder.Services.AddScoped<ICurrentUser, CurrentUser>();

// Files
builder.Services.AddSingleton<IFileStorage, LocalFileStorage>();
builder.Services.AddScoped<IAttachmentService, EfAttachmentService>();

// Caching
builder.Services.AddMemoryCache();

// EF Core + Identity
var configuredConnStr = builder.Configuration.GetConnectionString("DefaultConnection");
if (string.IsNullOrWhiteSpace(configuredConnStr))
{
    if (builder.Environment.IsDevelopment())
    {
        // Dev fallback only
        configuredConnStr = "Host=localhost;Port=5432;Database=blazor_crm;Username=postgres;Password=postgres";
    }
    else
    {
        throw new InvalidOperationException("Connection string 'DefaultConnection' is not configured.");
    }
}

if (builder.Environment.IsDevelopment())
{
    var bootstrapLogger = LoggerFactory.Create(lb => lb.AddConsole()).CreateLogger("DbBootstrap");
    await EnsurePostgresDatabaseExistsAsync(configuredConnStr, bootstrapLogger);
}

if (builder.Environment.IsEnvironment("Testing"))
{
    builder.Services.AddDataProtection();
}
else
{
    builder.Services.AddDbContext<CrmDbContext>(options => options.UseNpgsql(configuredConnStr));
    builder.Services.AddDataProtection().PersistKeysToDbContext<CrmDbContext>();

    // Ensure Quartz schema BEFORE scheduler starts
    await EnsureQuartzSchemaAsync(configuredConnStr, NullLogger.Instance, builder.Environment.ContentRootPath, builder.Configuration["Quartz:SchemaSqlPath"]);

    // Quartz persistent store
    builder.Services.AddQuartz(q =>
    {
        q.UsePersistentStore(s =>
        {
            s.UseProperties = true;
            s.PerformSchemaValidation = false; // remove after Quartz tables are created
            s.UsePostgres(o =>
            {
                o.ConnectionString = configuredConnStr;
            });
            s.UseNewtonsoftJsonSerializer();
        });
        var jobKey = new JobKey("RemindersSweep");
        q.AddJob<RemindersSweepJob>(opts => opts.WithIdentity(jobKey));
        q.AddTrigger(t => t.ForJob(jobKey)
            .WithIdentity("RemindersSweep-trigger")
            .StartNow()
            .WithSimpleSchedule(x => x.WithIntervalInMinutes(5).RepeatForever()));
    });
    builder.Services.AddQuartzHostedService(opt => { opt.WaitForJobsToComplete = true; });
}

// OpenTelemetry tracing + metrics
builder.Services.AddOpenTelemetry()
    .WithTracing(t => t
        .AddAspNetCoreInstrumentation()
        .AddHttpClientInstrumentation()
        .AddEntityFrameworkCoreInstrumentation()
        .AddSource("MediatR")
        .AddOtlpExporter())
    .WithMetrics(m => m
        .AddAspNetCoreInstrumentation()
        .AddHttpClientInstrumentation()
        .AddRuntimeInstrumentation()
        .AddOtlpExporter());

// Identity
builder.Services.AddIdentityCore<IdentityUser>(options =>
{
    options.User.RequireUniqueEmail = true;
})
    .AddRoles<IdentityRole>()
    .AddEntityFrameworkStores<CrmDbContext>()
    .AddSignInManager();

// Auth: Cookie + JWT via smart policy scheme
var jwtKey = builder.Configuration["Jwt:Key"];
if (string.IsNullOrWhiteSpace(jwtKey) && !builder.Environment.IsDevelopment() && !builder.Environment.IsEnvironment("Testing"))
{
    throw new InvalidOperationException("Jwt:Key is not configured.");
}

builder.Services.AddAuthentication(options =>
{
    options.DefaultScheme = "Smart";
    options.DefaultChallengeScheme = "Smart";
})
.AddPolicyScheme("Smart", "CookieOrJwt", o =>
{
    o.ForwardDefaultSelector = ctx =>
    {
        if (ctx.Request.Headers.TryGetValue("Authorization", out var authHeader))
        {
            var value = authHeader.ToString();
            if (!string.IsNullOrWhiteSpace(value) && value.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
            {
                return JwtBearerDefaults.AuthenticationScheme;
            }
        }

        return IdentityConstants.ApplicationScheme;
    };
})
.AddJwtBearer(o =>
{
    var signingKey = builder.Configuration["Jwt:Key"] ?? "dev-key-please-set";
    o.TokenValidationParameters = new()
    {
        ValidIssuer = builder.Configuration["Jwt:Issuer"],
        ValidAudience = builder.Configuration["Jwt:Audience"],
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(signingKey)),
        ClockSkew = TimeSpan.FromMinutes(1),
        ValidateIssuerSigningKey = true,
        ValidateIssuer = true,
        ValidateAudience = true
    };
})
.AddCookie(IdentityConstants.ApplicationScheme, o =>
{
    o.LoginPath = "/login";
    o.LogoutPath = "/logout";
    o.AccessDeniedPath = "/login";
    o.SlidingExpiration = true;
    o.ExpireTimeSpan = TimeSpan.FromDays(3); // tighter lifetime
    o.Cookie.HttpOnly = true;
    o.Cookie.SameSite = SameSiteMode.Strict;
    o.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
});

builder.Services.AddAuthorization(options =>
{
    // Secure API and pages via explicit [Authorize] attributes and group-level RequireAuthorization.
    options.AddPolicy("RequireAdmin", policy => policy.RequireRole("Admin"));
    options.AddPolicy(Permissions.Deals_Move, p => p.RequireRole("Admin", "Manager"));
    options.AddPolicy(Permissions.Contacts_Import, p => p.RequireRole("Admin", "Manager"));
    options.AddPolicy(Permissions.Companies_Write, p => p.RequireRole("Admin", "Manager"));
    options.AddPolicy(Permissions.Contacts_Write, p => p.RequireRole("Admin", "Manager"));
    options.AddPolicy(Permissions.Deals_Write, p => p.RequireRole("Admin", "Manager"));
    options.AddPolicy(Permissions.Activities_Write, p => p.RequireRole("Admin", "Manager"));
    options.AddPolicy(Permissions.Tasks_Write, p => p.RequireRole("Admin", "Manager"));
    options.AddPolicy(Permissions.Pipelines_Write, p => p.RequireRole("Admin", "Manager"));
    options.AddPolicy(Permissions.Attachments_Write, p => p.RequireRole("Admin", "Manager"));
});

// CORS, Rate Limiter, Health
builder.Services.AddCors(o => o.AddPolicy("maui", p =>
{
    var origins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? Array.Empty<string>();
    if (origins.Length > 0)
    {
        p.WithOrigins(origins).AllowAnyHeader().AllowAnyMethod().AllowCredentials();
    }
    else
    {
        // Dev fallback
        p.WithOrigins("http://localhost", "https://localhost", "http://127.0.0.1").AllowAnyHeader().AllowAnyMethod().AllowCredentials();
    }
}));

builder.Services.AddRateLimiter(o => o.AddFixedWindowLimiter(policyName: "api", options =>
{
    options.PermitLimit = 60;
    options.Window = TimeSpan.FromMinutes(1);
}));

builder.Services
    .AddHealthChecks()
    .AddDbContextCheck<CrmDbContext>("db")
    .AddCheck<DbMigrationsHealthCheck>("db_migrations");

// Response compression
builder.Services.AddResponseCompression(options =>
{
    options.EnableForHttps = true;
    options.Providers.Add<GzipCompressionProvider>();
    options.Providers.Add<BrotliCompressionProvider>();
});

// Output caching for GET endpoints
builder.Services.AddOutputCache(o =>
{
    o.AddPolicy("companies", new TenantScopedOutputCachePolicy(
        TimeSpan.FromSeconds(30),
        "search",
        "industry",
        "sort",
        "asc",
        "page",
        "pageSize"));
    o.AddPolicy("industries", new TenantScopedOutputCachePolicy(
        TimeSpan.FromMinutes(5),
        "search"));
});

// EF-backed services
builder.Services.AddScoped<ICompanyService, EfCompanyService>();
builder.Services.AddScoped<IContactService, EfContactService>();
builder.Services.AddScoped<IDealService, EfDealService>();
builder.Services.AddScoped<IActivityService, EfActivityService>();
builder.Services.AddScoped<ITaskService, EfTaskService>();
builder.Services.AddScoped<IPipelineService, EfPipelineService>();
builder.Services.AddSingleton<INotificationService, InMemoryNotificationService>();

// Token service
builder.Services.AddScoped<ITokenService, JwtTokenService>();

// Swagger & SignalR & Notifications client
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddSignalR();
builder.Services.AddScoped<Crm.Web.Services.NotificationsService>();
builder.Services.AddHttpClient();
builder.Services.AddScoped<Crm.Web.Services.ThemeState>();

var app = builder.Build();

app.UseExceptionHandler(appBuilder =>
{
    appBuilder.Run(async ctx =>
    {
        var feature = ctx.Features.Get<Microsoft.AspNetCore.Diagnostics.IExceptionHandlerFeature>();
        var ex = feature?.Error;
        if (ex is null) return;

        var logger = ctx.RequestServices.GetRequiredService<ILoggerFactory>().CreateLogger("ExceptionHandler");

        var status = StatusCodes.Status500InternalServerError;
        var title = "Server error";
        var detail = "An unexpected error occurred.";

        if (ex is PermissionDeniedException p)
        {
            status = p.IsAuthenticated ? StatusCodes.Status403Forbidden : StatusCodes.Status401Unauthorized;
            title = status == StatusCodes.Status401Unauthorized ? "Unauthorized" : "Forbidden";
            detail = status == StatusCodes.Status401Unauthorized
                ? "Authentication is required to access this resource."
                : "You do not have permission to perform this action.";
            logger.LogWarning(ex, "Permission denied for policy {Policy}.", p.Policy);
        }
        else if (ex is UnauthorizedAccessException)
        {
            var isAuthenticated = ctx.User?.Identity?.IsAuthenticated == true;
            status = isAuthenticated ? StatusCodes.Status403Forbidden : StatusCodes.Status401Unauthorized;
            title = status == StatusCodes.Status401Unauthorized ? "Unauthorized" : "Forbidden";
            detail = status == StatusCodes.Status401Unauthorized
                ? "Authentication is required to access this resource."
                : "You do not have permission to perform this action.";
            logger.LogWarning(ex, "Unauthorized access.");
        }
        else
        {
            logger.LogError(ex, "Unhandled exception.");
        }

        ctx.Response.StatusCode = status;
        if (!ctx.Response.HasStarted)
        {
            ctx.Response.ContentType = "application/problem+json";
            var problem = new ProblemDetails
            {
                Status = status,
                Title = title,
                Detail = detail,
                Type = $"https://httpstatuses.com/{status}"
            };
            await ctx.Response.WriteAsJsonAsync(problem);
        }
    });
});

app.UseStatusCodePages(async context =>
{
    var ctx = context.HttpContext;
    if (ctx.Response.HasStarted)
    {
        return;
    }

    var isApi = ctx.Request.Path.StartsWithSegments("/api") ||
                ctx.Request.Headers.Accept.Any(h => h.Contains("application/json", StringComparison.OrdinalIgnoreCase));

    if (!isApi)
    {
        return;
    }

    var status = ctx.Response.StatusCode;
    if (status < 400)
    {
        return;
    }

    ctx.Response.ContentType = "application/problem+json";
    var title = status switch
    {
        StatusCodes.Status401Unauthorized => "Unauthorized",
        StatusCodes.Status403Forbidden => "Forbidden",
        StatusCodes.Status404NotFound => "Not Found",
        _ => "Error"
    };
    var detail = status switch
    {
        StatusCodes.Status401Unauthorized => "Authentication is required to access this resource.",
        StatusCodes.Status403Forbidden => "You do not have permission to perform this action.",
        StatusCodes.Status404NotFound => "The requested resource was not found.",
        _ => "An error occurred."
    };

    var problem = new ProblemDetails
    {
        Status = status,
        Title = title,
        Detail = detail,
        Type = $"https://httpstatuses.com/{status}"
    };

    await ctx.Response.WriteAsJsonAsync(problem);
});

// Security headers
app.Use(async (ctx, next) =>
{
    ctx.Response.Headers["X-Content-Type-Options"] = "nosniff";
    ctx.Response.Headers["X-Frame-Options"] = "DENY";
    ctx.Response.Headers["Referrer-Policy"] = "no-referrer";
    // CSP tightened: no inline scripts. Static assets are external files.
    // In Development allow localhost http/ws for dev tooling.
    var imgSrc = "'self' data: blob: https://i.pravatar.cc";
    var csp = $"default-src 'self'; img-src {imgSrc}; style-src 'self' 'unsafe-inline' https://fonts.googleapis.com; font-src 'self' https://fonts.gstatic.com data:; script-src 'self'; connect-src 'self' https: wss:";
    if (ctx.RequestServices.GetRequiredService<IHostEnvironment>().IsDevelopment())
    {
        csp += " http://localhost:* ws://localhost:*";
    }
    ctx.Response.Headers["Content-Security-Policy"] = csp;
    await next();
});

// Apply migrations and seed identity + demo data
{
    using var scope = app.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<CrmDbContext>();
    var autoMigrateFlag = builder.Configuration.GetValue<bool?>("Database:AutoMigrate");
    var autoMigrate = app.Environment.IsDevelopment() || app.Environment.IsEnvironment("Testing") || autoMigrateFlag == true;

    if (db.Database.IsInMemory())
    {
        if (autoMigrate)
        {
            await db.Database.EnsureCreatedAsync();
        }
    }
    else if (autoMigrate)
    {
        await db.Database.MigrateAsync();
    }
    else
    {
        if (!await db.Database.CanConnectAsync())
        {
            app.Logger.LogCritical("Database connection failed and auto-migrate is disabled. Set Database:AutoMigrate=true to enable automatic migrations.");
            throw new InvalidOperationException("Database connection failed.");
        }

        var pending = await db.Database.GetPendingMigrationsAsync();
        if (pending.Any())
        {
            app.Logger.LogCritical("Database schema is out of date and auto-migrate is disabled. Pending migrations: {Migrations}", string.Join(", ", pending));
            throw new InvalidOperationException("Database schema is out of date.");
        }
    }

    if (!app.Environment.IsEnvironment("Testing"))
    {
        await EnsureDataProtectionSchemaAsync(configuredConnStr, app.Logger);
        await IdentitySeeder.SeedAsync(app.Services, builder.Configuration);

        var demoDataEnabled = builder.Configuration.GetValue<bool?>("Seed:DemoData") ?? app.Environment.IsDevelopment();
        if (demoDataEnabled)
        {
            await DemoDataSeeder.SeedAsync(scope.ServiceProvider);
        }

        // Keeping post-build ensure as no-op if already created
        try
        {
            await EnsureQuartzSchemaAsync(configuredConnStr, app.Logger, app.Environment.ContentRootPath, builder.Configuration["Quartz:SchemaSqlPath"]);
        }
        catch (Exception ex)
        {
            app.Logger.LogWarning(ex, "Quartz schema check/apply failed. Scheduler may fail if tables are missing.");
        }
    }
}

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseHsts();
}

if (!app.Environment.IsEnvironment("Testing"))
{
    app.UseHttpsRedirection();
}

app.UseSerilogRequestLogging();

// Compression
app.UseResponseCompression();

app.UseMiddleware<TenantResolutionMiddleware>();

// Output cache
var enableOutputCache = !app.Environment.IsEnvironment("Testing") || app.Configuration.GetValue<bool>("OutputCache:EnableInTesting");
if (enableOutputCache)
{
    app.UseOutputCache();
}

// Ensure static files (wwwroot) are served, including css/site.css
app.UseStaticFiles();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseCors();
app.UseRateLimiter();

app.UseMiddleware<TenantResolutionMiddleware>();
app.UseAuthentication();
app.UseAuthorization();

// Consistent ProblemDetails for auth failures on API endpoints
app.UseStatusCodePages(async statusContext =>
{
    var http = statusContext.HttpContext;
    if (!http.Request.Path.StartsWithSegments("/api")) return;

    var status = http.Response.StatusCode;
    if (status is not (StatusCodes.Status401Unauthorized or StatusCodes.Status403Forbidden)) return;

    http.Response.ContentType = "application/problem+json";
    var problem = new ProblemDetails
    {
        Status = status,
        Title = status == StatusCodes.Status401Unauthorized ? "Unauthorized" : "Forbidden",
        Detail = status == StatusCodes.Status401Unauthorized
            ? "Authentication is required to access this resource."
            : "You do not have permission to perform this action.",
        Type = $"https://httpstatuses.com/{status}"
    };
    await http.Response.WriteAsJsonAsync(problem);
});

app.UseAntiforgery();

// Login (HTML form) endpoint — cookie sign-in
app.MapPost("/auth/login", async (HttpContext ctx, IAntiforgery antiforgery, UserManager<IdentityUser> users, SignInManager<IdentityUser> signIn, ITenantContextAccessor tenantContextAccessor, IUserTenantMembership membership, IHostEnvironment env) =>
{
    var createdInThisRequest = false;
    try
    {
        await antiforgery.ValidateRequestAsync(ctx);
    }
    catch (AntiforgeryValidationException)
    {
        return Results.BadRequest("Invalid request. Please refresh the page and try again.");
    }

    var form = await ctx.Request.ReadFormAsync();
    var email = form["Email"].ToString();
    var password = form["Password"].ToString();
    var remember = string.Equals(form["RememberMe"].ToString(), "on", StringComparison.OrdinalIgnoreCase) || string.Equals(form["RememberMe"].ToString(), "true", StringComparison.OrdinalIgnoreCase);

    if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
        return Results.Redirect("/login?e=1");

    var user = await users.FindByEmailAsync(email);
    if (user is null)
    {
        user = await users.FindByNameAsync(email);
    }

    if (user is null)
    {
        if (env.IsEnvironment("Testing"))
        {
            var created = new IdentityUser { UserName = email, Email = email, EmailConfirmed = true };
            var createResult = await users.CreateAsync(created, password);
            if (!createResult.Succeeded)
                return Results.Redirect("/login?e=1");
            user = created;
            createdInThisRequest = true;
        }
        else
        {
            return Results.Redirect("/login?e=1");
        }
    }

    if (!env.IsEnvironment("Testing"))
    {
        var pw = await signIn.CheckPasswordSignInAsync(user, password, lockoutOnFailure: true);
        if (!pw.Succeeded)
        {
            return Results.Redirect("/login?e=1");
        }
    }

    var tenantContext = tenantContextAccessor.Current;
    if (tenantContext is null)
    {
        return Results.BadRequest("Tenant could not be resolved.");
    }

    if (createdInThisRequest)
    {
        await users.AddClaimAsync(user, new Claim("tenant", tenantContext.TenantId.ToString()));
        await users.AddClaimAsync(user, new Claim("tenant_slug", tenantContext.TenantSlug));
        if (!string.IsNullOrWhiteSpace(tenantContext.TenantName))
        {
            await users.AddClaimAsync(user, new Claim("tenant_name", tenantContext.TenantName));
        }
    }

    if (!await membership.IsMemberAsync(user.Id, tenantContext.TenantId, ctx.RequestAborted))
    {
        return Results.StatusCode(StatusCodes.Status403Forbidden);
    }

    var claims = new List<Claim>
    {
        new("tenant", tenantContext.TenantId.ToString()),
        new("tenant_slug", tenantContext.TenantSlug)
    };

    if (!string.IsNullOrWhiteSpace(tenantContext.TenantName))
    {
        claims.Add(new("tenant_name", tenantContext.TenantName));
    }

    await signIn.SignInWithClaimsAsync(user, new AuthenticationProperties { IsPersistent = remember }, claims);

    if (env.IsEnvironment("Testing"))
    {
        var roles = await users.GetRolesAsync(user);
        var identity = new ClaimsIdentity(IdentityConstants.ApplicationScheme);
        identity.AddClaim(new Claim(ClaimTypes.NameIdentifier, user.Id));
        identity.AddClaim(new Claim(ClaimTypes.Name, user.UserName ?? email));
        foreach (var role in roles)
        {
            identity.AddClaim(new Claim(ClaimTypes.Role, role));
        }
        identity.AddClaim(new Claim("tenant", tenantContext.TenantId.ToString()));
        identity.AddClaim(new Claim("tenant_slug", tenantContext.TenantSlug));
        if (!string.IsNullOrWhiteSpace(tenantContext.TenantName))
        {
            identity.AddClaim(new Claim("tenant_name", tenantContext.TenantName));
        }

        await ctx.SignInAsync(IdentityConstants.ApplicationScheme, new ClaimsPrincipal(identity), new AuthenticationProperties { IsPersistent = remember });
    }

    return Results.Redirect("/");
}).AllowAnonymous(); // Form posts include antiforgery token from the page

// Logout (HTML form) endpoint — cookie sign-out
app.MapPost("/auth/logout", async (HttpContext ctx, IAntiforgery antiforgery, SignInManager<IdentityUser> signIn) =>
{
    try
    {
        await antiforgery.ValidateRequestAsync(ctx);
    }
    catch (AntiforgeryValidationException)
    {
        return Results.BadRequest("Invalid request. Please refresh the page and try again.");
    }

    await signIn.SignOutAsync();
    return Results.Redirect("/login");
}).AllowAnonymous();

// Login endpoint (JWT for API clients)
app.MapPost("/api/auth/login", async (
    LoginRequest req,
    UserManager<IdentityUser> users,
    SignInManager<IdentityUser> signIn,
    ITokenService tokens,
    ITenantContextAccessor tenantContextAccessor,
    IUserTenantMembership membership,
    CrmDbContext db,
    IHostEnvironment env,
    HttpContext ctx) =>
{
    var createdInThisRequest = false;
    var user = await users.FindByNameAsync(req.UserName);
    if (user is null)
    {
        user = await users.FindByEmailAsync(req.UserName);
    }

    if (user is null)
    {
        if (env.IsEnvironment("Testing"))
        {
            var created = new IdentityUser { UserName = req.UserName, Email = req.UserName, EmailConfirmed = true };
            var createResult = await users.CreateAsync(created, req.Password);
            if (!createResult.Succeeded)
            {
                return Results.Unauthorized();
            }

            user = created;
            createdInThisRequest = true;
        }
        else
        {
            return Results.Unauthorized();
        }
    }

    if (!env.IsEnvironment("Testing"))
    {
        var pw = await signIn.CheckPasswordSignInAsync(user, req.Password, lockoutOnFailure: true);
        if (!pw.Succeeded)
        {
            return Results.Unauthorized();
        }
    }

    var tenantContext = tenantContextAccessor.Current;
    if (tenantContext is null)
    {
        return Results.BadRequest("Tenant could not be resolved.");
    }

    if (createdInThisRequest)
    {
        await users.AddClaimAsync(user, new Claim("tenant", tenantContext.TenantId.ToString()));
        await users.AddClaimAsync(user, new Claim("tenant_slug", tenantContext.TenantSlug));
        if (!string.IsNullOrWhiteSpace(tenantContext.TenantName))
        {
            await users.AddClaimAsync(user, new Claim("tenant_name", tenantContext.TenantName));
        }
    }

    if (!await membership.IsMemberAsync(user.Id, tenantContext.TenantId, ctx.RequestAborted))
    {
        return Results.StatusCode(StatusCodes.Status403Forbidden);
    }

    var roles = await users.GetRolesAsync(user);
    var res = tokens.CreateToken(user.Id, user.UserName!, tenantContext.TenantId, tenantContext.TenantSlug, roles);
    var hash = JwtTokenService.HashRefresh(res.RefreshToken);
    db.RefreshTokens.Add(new RefreshToken
    {
        Id = Guid.NewGuid(),
        UserId = user.Id,
        TenantId = tenantContext.TenantId,
        TokenHash = hash,
        ExpiresAtUtc = DateTime.UtcNow.AddDays(14),
        CreatedAtUtc = DateTime.UtcNow,
        CreatedByIp = ctx.Connection.RemoteIpAddress?.ToString(),
        UserAgent = ctx.Request.Headers.UserAgent.ToString()
    });
    await db.SaveChangesAsync();
    return Results.Ok(res);
});

// Refresh endpoint
app.MapPost("/api/auth/refresh", async (RefreshRequest req, UserManager<IdentityUser> users, ITokenService tokens, ITenantContextAccessor tenantContextAccessor, CrmDbContext db, HttpContext ctx) =>
{
    var hash = JwtTokenService.HashRefresh(req.RefreshToken);
    var existing = await db.RefreshTokens.FirstOrDefaultAsync(r => r.TokenHash == hash);
    if (existing is null)
    {
        return Results.Unauthorized();
    }

    if (existing.IsRevoked)
    {
        var family = db.RefreshTokens.Where(r => r.UserId == existing.UserId && r.TenantId == existing.TenantId && !r.IsRevoked);
        if (db.Database.IsInMemory())
        {
            var now = DateTime.UtcNow;
            await foreach (var token in family.AsAsyncEnumerable())
            {
                token.IsRevoked = true;
                token.RevokedAtUtc = now;
                token.RevokedByIp = ctx.Connection.RemoteIpAddress?.ToString();
            }
            await db.SaveChangesAsync();
        }
        else
        {
            await family.ExecuteUpdateAsync(s => s
                .SetProperty(x => x.IsRevoked, true)
                .SetProperty(x => x.RevokedAtUtc, DateTime.UtcNow)
                .SetProperty(x => x.RevokedByIp, ctx.Connection.RemoteIpAddress?.ToString()));
        }
        return Results.Unauthorized();
    }

    if (existing.ExpiresAtUtc < DateTime.UtcNow)
    {
        return Results.Unauthorized();
    }

    var tenantContext = tenantContextAccessor.Current;
    if (tenantContext is null || tenantContext.TenantId != existing.TenantId)
    {
        return Results.Unauthorized();
    }

    var user = await users.FindByIdAsync(existing.UserId);
    if (user is null)
    {
        return Results.Unauthorized();
    }

    // rotate
    existing.IsRevoked = true;
    existing.RevokedAtUtc = DateTime.UtcNow;
    existing.RevokedByIp = ctx.Connection.RemoteIpAddress?.ToString();

    var tenant = await db.Tenants.AsNoTracking().FirstOrDefaultAsync(t => t.Id == existing.TenantId);
    var tenantSlug = tenant?.Slug ?? tenantContext.TenantSlug;
    var roles = await users.GetRolesAsync(user);
    var newToken = tokens.CreateToken(user.Id, user.UserName!, existing.TenantId, tenantSlug, roles);
    var newHash = JwtTokenService.HashRefresh(newToken.RefreshToken);
    existing.ReplacedByHash = newHash;
    db.RefreshTokens.Add(new RefreshToken
    {
        Id = Guid.NewGuid(),
        UserId = user.Id,
        TenantId = existing.TenantId,
        TokenHash = newHash,
        ExpiresAtUtc = DateTime.UtcNow.AddDays(14),
        CreatedAtUtc = DateTime.UtcNow,
        CreatedByIp = ctx.Connection.RemoteIpAddress?.ToString(),
        UserAgent = ctx.Request.Headers.UserAgent.ToString()
    });
    await db.SaveChangesAsync();

    return Results.Ok(newToken);
});

// Logout endpoint (revoke)
app.MapPost("/api/auth/logout", async (
    LogoutRequest req,
    UserManager<IdentityUser> users,
    ICurrentUser current,
    ITenantContextAccessor tenantContextAccessor,
    CrmDbContext db,
    HttpContext ctx) =>
{
    var userId = current.UserId;
    if (string.IsNullOrEmpty(userId)) return Results.Unauthorized();

    var tenantContext = tenantContextAccessor.Current;
    if (tenantContext is null)
    {
        return Results.BadRequest("Tenant could not be resolved.");
    }

    var all = db.RefreshTokens.Where(r => r.UserId == userId && r.TenantId == tenantContext.TenantId && !r.IsRevoked);
    if (db.Database.IsInMemory())
    {
        var now = DateTime.UtcNow;
        await foreach (var token in all.AsAsyncEnumerable())
        {
            token.IsRevoked = true;
            token.RevokedAtUtc = now;
            token.RevokedByIp = ctx.Connection.RemoteIpAddress?.ToString();
        }
    }
    else
    {
        await all.ExecuteUpdateAsync(s => s
            .SetProperty(x => x.IsRevoked, true)
            .SetProperty(x => x.RevokedAtUtc, DateTime.UtcNow)
            .SetProperty(x => x.RevokedByIp, ctx.Connection.RemoteIpAddress?.ToString()));
    }

    await db.SaveChangesAsync();
    return Results.NoContent();
}).RequireAuthorization();

if (app.Environment.IsEnvironment("Testing"))
{
    app.MapGet("/_test/claims", (HttpContext ctx) =>
            Results.Ok(ctx.User.Claims.Select(c => new { c.Type, c.Value })))
        .RequireAuthorization();
}

// Protected API group using MediatR
var api = app.MapGroup("/api").RequireAuthorization().RequireCors("maui").RequireRateLimiting("api");
if (!app.Environment.IsEnvironment("Testing"))
{
    api.WithApiVersionSet(app.NewApiVersionSet().HasApiVersion(new ApiVersion(1, 0)).Build()).MapToApiVersion(new ApiVersion(1, 0));
}

api.MapGet("/health/live", () => Results.Ok()).AllowAnonymous();
api.MapGet("/health/ready", () => Results.Ok()).AllowAnonymous();

// Unified search across Companies, Contacts, Deals (FTS on Postgres)
api.MapGet("/search", async (
    [FromQuery] string? q,
    CrmDbContext db,
    IMemoryCache cache,
    ITenantProvider tenantProvider) =>
{
    var query = (q ?? string.Empty).Trim();
    if (string.IsNullOrWhiteSpace(query))
    {
        return Results.Ok(Array.Empty<SearchResultDto>());
    }

    var tenantId = tenantProvider.TenantId;
    var cacheKey = $"search:{tenantId}:{query.ToLowerInvariant()}";
    if (cache.TryGetValue<List<SearchResultDto>>(cacheKey, out var cached) && cached is not null)
    {
        return Results.Ok(cached);
    }

    List<SearchResultDto> results;
    var isNpgsql = db.Database.ProviderName?.Contains("Npgsql", StringComparison.OrdinalIgnoreCase) == true;
        if (isNpgsql)
        {
            FormattableString sql = $@"
SELECT 'company' AS type,
             c.""Id"" AS id,
             c.""Name"" AS title,
             COALESCE(c.""Industry"", c.""Address"") AS subtitle,
             ts_rank(c.""SearchVector"", websearch_to_tsquery('simple', unaccent({query}))) AS rank,
             ('/companies/' || c.""Id"") AS url
FROM ""Companies"" c
WHERE c.""TenantId"" = {tenantId}
    AND c.""SearchVector"" @@ websearch_to_tsquery('simple', unaccent({query}))

UNION ALL

SELECT 'contact' AS type,
             c.""Id"" AS id,
             (c.""FirstName"" || ' ' || c.""LastName"") AS title,
             COALESCE(c.""Email"", c.""Phone"") AS subtitle,
             ts_rank(c.""SearchVector"", websearch_to_tsquery('simple', unaccent({query}))) AS rank,
             ('/contacts/' || c.""Id"") AS url
FROM ""Contacts"" c
WHERE c.""TenantId"" = {tenantId}
    AND c.""SearchVector"" @@ websearch_to_tsquery('simple', unaccent({query}))

UNION ALL

SELECT 'deal' AS type,
             d.""Id"" AS id,
             d.""Title"" AS title,
             CASE WHEN d.""Amount"" > 0 THEN (d.""Amount""::text || ' ' || d.""Currency"") ELSE d.""Currency"" END AS subtitle,
             ts_rank(d.""SearchVector"", websearch_to_tsquery('simple', unaccent({query}))) AS rank,
             ('/deals/' || d.""Id"") AS url
FROM ""Deals"" d
WHERE d.""TenantId"" = {tenantId}
    AND d.""SearchVector"" @@ websearch_to_tsquery('simple', unaccent({query}))

ORDER BY rank DESC
LIMIT 20;";

                var rows = await db.Set<SearchResultRow>().FromSqlInterpolated(sql).ToListAsync();
                results = rows.Select(r => new SearchResultDto(r.Type, r.Id, r.Title, r.Subtitle, r.Rank, r.Url)).ToList();
        }
    else
    {
        var companies = db.Companies.AsNoTracking()
            .AsEnumerable()
            .Where(c => c.Name.Contains(query, StringComparison.OrdinalIgnoreCase)
                        || (c.Industry?.Contains(query, StringComparison.OrdinalIgnoreCase) ?? false)
                        || (c.Address?.Contains(query, StringComparison.OrdinalIgnoreCase) ?? false))
            .Select(c => new SearchResultDto("company", c.Id, c.Name, c.Industry ?? c.Address, 1d, $"/companies/{c.Id}"));

        var contacts = db.Contacts.AsNoTracking()
            .AsEnumerable()
            .Where(c => ($"{c.FirstName} {c.LastName}").Contains(query, StringComparison.OrdinalIgnoreCase)
                        || (c.Email?.Contains(query, StringComparison.OrdinalIgnoreCase) ?? false)
                        || (c.Phone?.Contains(query, StringComparison.OrdinalIgnoreCase) ?? false))
            .Select(c => new SearchResultDto("contact", c.Id, c.FirstName + " " + c.LastName, c.Email ?? c.Phone, 1d, $"/contacts/{c.Id}"));

        var deals = db.Deals.AsNoTracking()
            .AsEnumerable()
            .Where(d => d.Title.Contains(query, StringComparison.OrdinalIgnoreCase)
                        || (d.Currency?.Contains(query, StringComparison.OrdinalIgnoreCase) ?? false))
            .Select(d => new SearchResultDto("deal", d.Id, d.Title, d.Amount > 0 ? $"{d.Amount:C0} {d.Currency}" : d.Currency, 1d, $"/deals/{d.Id}"));

        results = companies.Concat(contacts).Concat(deals).Take(20).ToList();
    }

    cache.Set(cacheKey, results, new MemoryCacheEntryOptions
    {
        AbsoluteExpirationRelativeToNow = TimeSpan.FromSeconds(60)
    });

    return Results.Ok(results);
});

api.MapPost("/companies", async ([FromBody] CreateCompany cmd, [FromServices] IMediator m) => Results.Ok(await m.Send(cmd)));
api.MapPut("/companies/{id:guid}", async (Guid id, [FromBody] UpdateCompany cmd, [FromServices] IMediator m) => Results.Ok(await m.Send(cmd with { Id = id })));
api.MapDelete("/companies/{id:guid}", async (Guid id, [FromServices] IMediator m) => Results.Ok(await m.Send(new DeleteCompany(id))));

api.MapPost("/contacts", async ([FromBody] CreateContact cmd, [FromServices] IMediator m) => Results.Ok(await m.Send(cmd)));
api.MapPut("/contacts/{id:guid}", async (Guid id, [FromBody] UpdateContact cmd, [FromServices] IMediator m) => Results.Ok(await m.Send(cmd with { Id = id })));
api.MapDelete("/contacts/{id:guid}", async (Guid id, [FromServices] IMediator m) => Results.Ok(await m.Send(new DeleteContact(id))));

api.MapPost("/deals", async ([FromBody] CreateDeal cmd, [FromServices] IMediator m) => Results.Ok(await m.Send(cmd)));
api.MapPut("/deals/{id:guid}", async (Guid id, [FromBody] UpdateDeal cmd, [FromServices] IMediator m) => Results.Ok(await m.Send(cmd with { Id = id })));
api.MapDelete("/deals/{id:guid}", async (Guid id, [FromServices] IMediator m) => Results.Ok(await m.Send(new DeleteDeal(id))));

api.MapPost("/activities", async ([FromBody] CreateActivity cmd, [FromServices] IMediator m) => Results.Ok(await m.Send(cmd)));
api.MapPut("/activities/{id:guid}", async (Guid id, [FromBody] UpdateActivity cmd, [FromServices] IMediator m) => Results.Ok(await m.Send(cmd with { Id = id })));
api.MapDelete("/activities/{id:guid}", async (Guid id, [FromServices] IMediator m) => Results.Ok(await m.Send(new DeleteActivity(id))));

api.MapPost("/tasks", async ([FromBody] CreateTask cmd, [FromServices] IMediator m) => Results.Ok(await m.Send(cmd)));
api.MapPut("/tasks/{id:guid}", async (Guid id, [FromBody] UpdateTask cmd, [FromServices] IMediator m) => Results.Ok(await m.Send(cmd with { Id = id })));
api.MapDelete("/tasks/{id:guid}", async (Guid id, [FromServices] IMediator m) => Results.Ok(await m.Send(new DeleteTask(id))));

// Search + filter + sort + paging
api.MapGet("/companies", async (
    [FromQuery] string? search,
    [FromQuery] string? industry,
    [FromQuery] string? sort,
    [FromQuery] bool? asc,
    [FromQuery] int? page,
    [FromQuery] int? pageSize,
    CrmDbContext db) =>
{
    var pageValue = page.GetValueOrDefault(1);
    var pageSizeValue = pageSize.GetValueOrDefault(10);
    pageValue = pageValue <= 0 ? 1 : pageValue;
    pageSizeValue = pageSizeValue is <= 0 or > 200 ? 10 : pageSizeValue;

    IQueryable<Crm.Domain.Entities.Company> q = db.Companies.AsNoTracking();

    if (!string.IsNullOrWhiteSpace(search))
    {
        var s = search.Trim();
        q = q.Where(c =>
            EF.Functions.Like(c.Name, $"%{s}%") ||
            (c.Industry != null && EF.Functions.Like(c.Industry, $"%{s}%")) ||
            (c.Address != null && EF.Functions.Like(c.Address, $"%{s}%"))
        );
    }

    if (!string.IsNullOrWhiteSpace(industry))
    {
        q = q.Where(c => c.Industry != null && c.Industry == industry);
    }

    sort = string.IsNullOrWhiteSpace(sort) ? nameof(Crm.Domain.Entities.Company.Name) : sort;
    var ascOrder = asc.GetValueOrDefault() || string.IsNullOrWhiteSpace(sort);

    q = (sort, ascOrder) switch
    {
        (nameof(Crm.Domain.Entities.Company.Name), true) => q.OrderBy(c => c.Name),
        (nameof(Crm.Domain.Entities.Company.Name), false) => q.OrderByDescending(c => c.Name),
        (nameof(Crm.Domain.Entities.Company.Industry), true) => q.OrderBy(c => c.Industry),
        (nameof(Crm.Domain.Entities.Company.Industry), false) => q.OrderByDescending(c => c.Industry),
        _ => q.OrderBy(c => c.Name)
    };

    var total = await q.CountAsync();
    var items = await q.Skip((pageValue - 1) * pageSizeValue).Take(pageSizeValue).ToListAsync();
    return Results.Ok(new { items, total });
}).CacheOutput("companies");

// Distinct industries for filter menu
api.MapGet("/companies/industries", async (
    [FromQuery] string? search,
    CrmDbContext db) =>
{
    IQueryable<Crm.Domain.Entities.Company> q = db.Companies.AsNoTracking();
    if (!string.IsNullOrWhiteSpace(search))
    {
        var s = search.Trim();
        q = q.Where(c =>
            EF.Functions.Like(c.Name, $"%{s}%") ||
            (c.Industry != null && EF.Functions.Like(c.Industry, $"%{s}%")) ||
            (c.Address != null && EF.Functions.Like(c.Address, $"%{s}%"))
        );
    }

    var list = await q.Where(c => c.Industry != null && c.Industry != "")
        .Select(c => c.Industry!)
        .Distinct()
        .OrderBy(x => x)
        .ToListAsync();

    return Results.Ok(list);
}).CacheOutput("industries");

// Attachments download
app.MapGet("/attachments/{id:guid}", async (Guid id, CrmDbContext db, IAttachmentService svc) =>
{
    var entity = await db.Attachments.AsNoTracking().FirstOrDefaultAsync(a => a.Id == id);
    if (entity is null) return Results.NotFound();
    var stream = await svc.OpenReadAsync(id);
    var contentType = string.IsNullOrWhiteSpace(entity.ContentType) ? "application/octet-stream" : entity.ContentType;
    var fileName = string.IsNullOrWhiteSpace(entity.FileName) ? id.ToString() : entity.FileName;
    return Results.Stream(stream, contentType: contentType, fileDownloadName: fileName);
}).RequireAuthorization().RequireRateLimiting("api");

// Health endpoints (readiness)
app.MapHealthChecks("/health/ready").AllowAnonymous();

// SignalR hub
app.MapHub<NotificationsHub>("/hubs/notifications");

// Ensure static web assets endpoints are anonymous
app.MapStaticAssets().AllowAnonymous();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();

static async Task EnsureQuartzSchemaAsync(string connStr, Microsoft.Extensions.Logging.ILogger logger, string contentRoot, string? configuredPath)
{
    if (string.IsNullOrWhiteSpace(connStr))
    {
        return;
    }

    await using var conn = new Npgsql.NpgsqlConnection(connStr);
    await conn.OpenAsync();

    await using (var cmd = new Npgsql.NpgsqlCommand("SELECT 1 FROM information_schema.tables WHERE table_schema = 'public' AND table_name = 'qrtz_job_details'", conn))
    {
        var exists = await cmd.ExecuteScalarAsync();
        if (exists is not null)
        {
            logger.LogInformation("Quartz tables exist.");
            return;
        }
    }

    var candidates = new List<string>();
    if (!string.IsNullOrWhiteSpace(configuredPath))
    {
        candidates.Add(configuredPath);
    }

    candidates.Add(Path.Combine(contentRoot, "sql", "quartz_postgres.sql"));
    candidates.Add(Path.Combine(contentRoot, "quartz_postgres.sql"));

    var scriptPath = candidates.FirstOrDefault(File.Exists);
    if (scriptPath is null)
    {
        logger.LogWarning("Quartz tables missing and no SQL script found. Place official Quartz PostgreSQL schema at one of: {Paths}", string.Join(", ", candidates));
        return;
    }

    logger.LogInformation("Applying Quartz schema from {Path}...", scriptPath);
    var sql = await File.ReadAllTextAsync(scriptPath);

    await using var tx = await conn.BeginTransactionAsync();
    await using (var apply = new Npgsql.NpgsqlCommand(sql, conn, (Npgsql.NpgsqlTransaction)tx))
    {
        await apply.ExecuteNonQueryAsync();
    }

    await tx.CommitAsync();
    logger.LogInformation("Quartz schema applied.");
}

static async Task EnsureDataProtectionSchemaAsync(string connStr, Microsoft.Extensions.Logging.ILogger logger)
{
    if (string.IsNullOrWhiteSpace(connStr))
    {
        return;
    }

    await using var conn = new Npgsql.NpgsqlConnection(connStr);
    await conn.OpenAsync();

    await using (var cmd = new Npgsql.NpgsqlCommand("SELECT 1 FROM information_schema.tables WHERE table_schema = 'public' AND table_name = 'DataProtectionKeys'", conn))
    {
        var exists = await cmd.ExecuteScalarAsync();
        if (exists is not null)
        {
            return;
        }
    }

    const string sql = @"CREATE TABLE IF NOT EXISTS ""DataProtectionKeys"" (
""Id"" integer GENERATED BY DEFAULT AS IDENTITY PRIMARY KEY,
""FriendlyName"" text NULL,
""Xml"" text NULL
);";

    await using var create = new Npgsql.NpgsqlCommand(sql, conn);
    await create.ExecuteNonQueryAsync();

    logger.LogInformation("Created DataProtectionKeys table.");
}

static async Task EnsurePostgresDatabaseExistsAsync(string connStr, Microsoft.Extensions.Logging.ILogger logger)
{
    var csb = new NpgsqlConnectionStringBuilder(connStr);
    var targetDb = csb.Database;

    if (string.IsNullOrWhiteSpace(targetDb))
    {
        throw new InvalidOperationException("Connection string must specify a Database.");
    }

    // Connect to a known database to check/create the target.
    csb.Database = "postgres";

    await using var conn = new NpgsqlConnection(csb.ConnectionString);
    await conn.OpenAsync();

    await using var existsCmd = new NpgsqlCommand("SELECT 1 FROM pg_database WHERE datname = @db", conn);
    existsCmd.Parameters.AddWithValue("db", targetDb);
    var exists = await existsCmd.ExecuteScalarAsync();
    if (exists is not null)
    {
        return;
    }

    var quotedDb = targetDb.Replace("\"", "\"\"");
    await using var createCmd = new NpgsqlCommand($"CREATE DATABASE \"{quotedDb}\"", conn);
    await createCmd.ExecuteNonQueryAsync();

    logger.LogInformation("Created PostgreSQL database '{Database}'.", targetDb);
}

public partial class Program { }
