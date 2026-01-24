namespace Crm.Infrastructure.Persistence
{
    using Crm.Application.Common.Abstractions;
    using Crm.Application.Common.Multitenancy;
    using Crm.Domain.Common;
    using Crm.Domain.Entities;
    using Crm.Infrastructure.Auditing;
    using Crm.Infrastructure.Security;
    using Microsoft.AspNetCore.Identity;
    using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
    using Microsoft.EntityFrameworkCore;
    using Microsoft.AspNetCore.DataProtection.EntityFrameworkCore;
    using Microsoft.EntityFrameworkCore.ChangeTracking; // added for ValueComparer
    using NpgsqlTypes;

    public class CrmDbContext : IdentityDbContext<IdentityUser, IdentityRole, string>, IDataProtectionKeyContext
    {
        private readonly ITenantProvider _tenantProvider;
        private readonly ICurrentUser? _currentUser;

        public CrmDbContext(DbContextOptions<CrmDbContext> options, ITenantProvider tenantProvider, ICurrentUser? currentUser = null) : base(options)
        {
            _tenantProvider = tenantProvider;
            _currentUser = currentUser;
        }

        public DbSet<DataProtectionKey> DataProtectionKeys { get; set; } = default!;

        public DbSet<Tenant> Tenants => Set<Tenant>();

        public DbSet<Company> Companies => Set<Company>();

        public DbSet<Contact> Contacts => Set<Contact>();

        public DbSet<Pipeline> Pipelines => Set<Pipeline>();

        public DbSet<Stage> Stages => Set<Stage>();

        public DbSet<Deal> Deals => Set<Deal>();

        public DbSet<Activity> Activities => Set<Activity>();

        public DbSet<TaskItem> Tasks => Set<TaskItem>();

        public DbSet<Attachment> Attachments => Set<Attachment>();

        public DbSet<Team> Teams => Set<Team>();

        public DbSet<UserTeam> UserTeams => Set<UserTeam>();

        public DbSet<AuditEntry> AuditEntries => Set<AuditEntry>();

        public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            var isNpgsql = Database.IsNpgsql();

            foreach (var entityType in builder.Model.GetEntityTypes())
            {
                if (typeof(BaseEntity).IsAssignableFrom(entityType.ClrType))
                {
                    builder.Entity(entityType.ClrType).Property<string>(nameof(BaseEntity.ConcurrencyStamp)).IsConcurrencyToken();
                }
            }

            var stringListComparer = new ValueComparer<List<string>>(
                (a, b) => (a == null && b == null) || (a != null && b != null && a.SequenceEqual(b)),
                c => c == null ? 0 : c.Aggregate(0, (acc, v) => HashCode.Combine(acc, v.GetHashCode())),
                c => c == null ? new List<string>() : c.ToList());

            builder.Entity<AuditEntry>(b =>
            {
                b.HasKey(x => x.Id);
                b.Property(x => x.Entity).HasMaxLength(200);
                b.Property(x => x.Action).HasMaxLength(50);
                b.HasIndex(x => new { x.TenantId, x.Entity, x.EntityId, x.CreatedAtUtc });
            });

            builder.Entity<Tenant>(b =>
            {
                b.Property(x => x.Name).IsRequired().HasMaxLength(200);
                b.Property(x => x.Slug).IsRequired().HasMaxLength(100);
                b.HasIndex(x => x.Slug).IsUnique();
            });

            builder.Entity<Company>(b =>
            {
                b.Property(x => x.Name).IsRequired().HasMaxLength(200);
                b.Property(x => x.Industry).HasMaxLength(100);
                b.Property(x => x.Address).HasMaxLength(500);
                b.HasIndex(x => new { x.TenantId, x.CreatedAtUtc });
                b.HasIndex(x => new { x.TenantId, x.Name });
                var prop = b.Property(x => x.Tags)
                    .HasConversion(
                        v => string.Join(',', v ?? new List<string>()),
                        v => string.IsNullOrWhiteSpace(v) ? new List<string>() : v.Split(',', StringSplitOptions.RemoveEmptyEntries).Select(t => t.Trim()).ToList());
                prop.Metadata.SetValueComparer(stringListComparer);
                if (isNpgsql)
                {
                    b.Property<NpgsqlTsVector>("SearchVector")
                        .HasColumnType("tsvector");
                    b.HasIndex("SearchVector").HasMethod("GIN");
                }
                else
                {
                    b.Ignore("SearchVector");
                }
                b.HasQueryFilter(e => e.TenantId == _tenantProvider.TenantId);
            });

            builder.Entity<Contact>(b =>
            {
                b.Property(x => x.FirstName).IsRequired().HasMaxLength(100);
                b.Property(x => x.LastName).IsRequired().HasMaxLength(100);
                b.Property(x => x.Email).HasMaxLength(256);
                b.Property(x => x.Phone).HasMaxLength(50);
                b.HasIndex(x => new { x.TenantId, x.CreatedAtUtc });
                b.HasIndex(x => new { x.TenantId, x.Email });
                var prop = b.Property(x => x.Tags)
                    .HasConversion(
                        v => string.Join(',', v ?? new List<string>()),
                        v => string.IsNullOrWhiteSpace(v) ? new List<string>() : v.Split(',', StringSplitOptions.RemoveEmptyEntries).Select(t => t.Trim()).ToList());
                prop.Metadata.SetValueComparer(stringListComparer);
                if (isNpgsql)
                {
                    b.Property<NpgsqlTsVector>("SearchVector")
                        .HasColumnType("tsvector");
                    b.HasIndex("SearchVector").HasMethod("GIN");
                }
                else
                {
                    b.Ignore("SearchVector");
                }
                b.HasQueryFilter(e => e.TenantId == _tenantProvider.TenantId);
            });

            builder.Entity<Pipeline>(b =>
            {
                b.Property(x => x.Name).IsRequired().HasMaxLength(100);
                b.HasQueryFilter(e => e.TenantId == _tenantProvider.TenantId);
            });

            builder.Entity<Stage>(b =>
            {
                b.Property(x => x.Name).IsRequired().HasMaxLength(100);
                b.HasIndex(x => new { x.PipelineId, x.Order }).IsUnique();
                b.HasQueryFilter(e => e.TenantId == _tenantProvider.TenantId);
            });

            builder.Entity<Deal>(b =>
            {
                b.Property(x => x.Title).IsRequired().HasMaxLength(200);
                b.Property(x => x.Currency).IsRequired().HasMaxLength(10);
                b.HasIndex(x => new { x.TenantId, x.CreatedAtUtc });
                if (isNpgsql)
                {
                    b.Property<NpgsqlTsVector>("SearchVector")
                        .HasColumnType("tsvector");
                    b.HasIndex("SearchVector").HasMethod("GIN");
                }
                else
                {
                    b.Ignore("SearchVector");
                }
                b.HasIndex(x => new { x.TenantId, x.StageId });
                b.HasIndex(x => new { x.TenantId, x.OwnerId });
                b.HasIndex(x => new { x.TenantId, x.CompanyId });
                b.HasIndex(x => new { x.TenantId, x.ContactId });
                b.HasQueryFilter(e => e.TenantId == _tenantProvider.TenantId);
            });

            builder.Entity<Activity>(b =>
            {
                b.Property(x => x.Type).IsRequired();
                b.Property(x => x.Status).IsRequired();
                b.Property(x => x.Notes).HasMaxLength(1000);
                b.Property(x => x.DueAt);
                b.Property(x => x.RelatedTo).IsRequired();
                b.Property(x => x.RelatedId);
                b.Property(x => x.OwnerId);
                b.HasIndex(x => new { x.TenantId, x.RelatedId });
                b.HasIndex(x => new { x.TenantId, x.CreatedAtUtc });
                b.HasQueryFilter(e => e.TenantId == _tenantProvider.TenantId);
            });

            builder.Entity<TaskItem>(b =>
            {
                b.Property(x => x.Title).IsRequired().HasMaxLength(200);
                b.HasIndex(x => new { x.TenantId, x.RelatedId });
                b.HasIndex(x => new { x.TenantId, x.CreatedAtUtc });
                b.HasQueryFilter(e => e.TenantId == _tenantProvider.TenantId);
            });

            builder.Entity<Attachment>(b =>
            {
                b.HasIndex(x => new { x.TenantId, x.RelatedTo, x.RelatedId });
                b.HasQueryFilter(e => e.TenantId == _tenantProvider.TenantId);
            });

            builder.Entity<Team>(b =>
            {
                b.Property(x => x.Name).IsRequired().HasMaxLength(100);
            });

            builder.Entity<UserTeam>(b =>
            {
                b.HasIndex(x => new { x.UserId, x.TeamId }).IsUnique();
            });

            builder.Entity<RefreshToken>(b =>
            {
                b.HasKey(x => x.Id);
                b.Property(x => x.UserId).IsRequired();
                b.Property(x => x.TokenHash).IsRequired();
                b.Property(x => x.TenantId).IsRequired();
                b.Property(x => x.CreatedAtUtc).IsRequired();
                b.HasIndex(x => new { x.UserId, x.TokenHash }).IsUnique();
                b.HasIndex(x => new { x.UserId, x.TenantId, x.IsRevoked });
                b.HasIndex(x => x.ExpiresAtUtc);
            });

            builder.Entity<SearchResultRow>(b =>
            {
                b.HasNoKey();
                b.ToView(null);
            });
        }

        public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            var tenantOwnedEntries = ChangeTracker.Entries<ITenantOwned>()
                .Where(entry => entry.State == EntityState.Added && entry.Entity.TenantId == Guid.Empty)
                .ToList();

            if (tenantOwnedEntries.Count > 0)
            {
                var tenantIdCurrentForNew = _tenantProvider.TenantId;
                foreach (var entry in tenantOwnedEntries)
                {
                    entry.Entity.TenantId = tenantIdCurrentForNew;
                }
            }

            foreach (var entry in ChangeTracker.Entries<Crm.Domain.Common.IHasCreatedAt>())
            {
                if (entry.State == EntityState.Added && entry.Entity.CreatedAtUtc == default)
                {
                    entry.Entity.CreatedAtUtc = DateTime.UtcNow;
                }
            }

            ChangeTracker.DetectChanges();
            Guid tenantIdCurrent = Guid.Empty;
            var needsAuditFallbackTenant = ChangeTracker.Entries()
                .Any(e => e.Entity is BaseEntity && e.State is EntityState.Added or EntityState.Modified or EntityState.Deleted && e.Entity is not ITenantOwned);
            if (needsAuditFallbackTenant)
            {
                try
                {
                    tenantIdCurrent = _tenantProvider.TenantId;
                }
                catch
                {
                    tenantIdCurrent = Guid.Empty;
                }
            }
            var now = DateTime.UtcNow;
            var audits = new List<AuditEntry>();
            foreach (var entry in ChangeTracker.Entries().Where(e => e.Entity is BaseEntity && e.State is EntityState.Added or EntityState.Modified or EntityState.Deleted))
            {
                var be = (BaseEntity)entry.Entity;
                var entityName = entry.Entity.GetType().Name;
                var action = entry.State.ToString();
                var changes = new Dictionary<string, object?>();
                if (entry.State == EntityState.Modified)
                {
                    foreach (var prop in entry.Properties.Where(p => p.IsModified))
                    {
                        if (prop.Metadata.IsConcurrencyToken) continue;
                        changes[prop.Metadata.Name] = new { Old = prop.OriginalValue, New = prop.CurrentValue };
                    }
                }

                audits.Add(new AuditEntry
                {
                    Id = Guid.NewGuid(),
                    TenantId = (entry.Entity is ITenantOwned to) ? to.TenantId : tenantIdCurrent,
                    UserId = _currentUser?.UserId,
                    Entity = entityName,
                    EntityId = be.Id.ToString(),
                    Action = action,
                    ChangesJson = changes.Count == 0 ? null : System.Text.Json.JsonSerializer.Serialize(changes),
                    CreatedAtUtc = now,
                    CorrelationId = _currentUser?.CorrelationId
                });
            }

            if (audits.Count > 0)
            {
                await AuditEntries.AddRangeAsync(audits, cancellationToken);
            }

            return await base.SaveChangesAsync(cancellationToken);
        }
    }
}
