namespace Crm.Infrastructure.Persistence
{
    using Crm.Application.Common.Multitenancy;
    using Crm.Domain.Common;
    using Crm.Domain.Entities;
    using Microsoft.AspNetCore.Identity;
    using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
    using Microsoft.EntityFrameworkCore;

    public class CrmDbContext : IdentityDbContext<IdentityUser, IdentityRole, string>
    {
        private readonly ITenantProvider _tenantProvider;

        public CrmDbContext(DbContextOptions<CrmDbContext> options, ITenantProvider tenantProvider) : base(options)
        {
            _tenantProvider = tenantProvider;
        }

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

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            foreach (var entityType in builder.Model.GetEntityTypes())
            {
                if (typeof(BaseEntity).IsAssignableFrom(entityType.ClrType))
                {
                    builder.Entity(entityType.ClrType).Property<string>(nameof(BaseEntity.ConcurrencyStamp)).IsConcurrencyToken();
                }
            }

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
                b.Property(x => x.Tags)
                    .HasConversion(
                        v => string.Join(',', v ?? new List<string>()),
                        v => string.IsNullOrWhiteSpace(v) ? new List<string>() : v.Split(',', StringSplitOptions.RemoveEmptyEntries).Select(t => t.Trim()).ToList());
                b.HasQueryFilter(e => e.TenantId == _tenantProvider.TenantId);
            });

            builder.Entity<Contact>(b =>
            {
                b.Property(x => x.FirstName).IsRequired().HasMaxLength(100);
                b.Property(x => x.LastName).IsRequired().HasMaxLength(100);
                b.Property(x => x.Email).HasMaxLength(256);
                b.Property(x => x.Phone).HasMaxLength(50);
                b.Property(x => x.Tags)
                    .HasConversion(
                        v => string.Join(',', v ?? new List<string>()),
                        v => string.IsNullOrWhiteSpace(v) ? new List<string>() : v.Split(',', StringSplitOptions.RemoveEmptyEntries).Select(t => t.Trim()).ToList());
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
                b.HasQueryFilter(e => e.TenantId == _tenantProvider.TenantId);
            });

            builder.Entity<Activity>(b =>
            {
                b.HasQueryFilter(e => e.TenantId == _tenantProvider.TenantId);
            });

            builder.Entity<TaskItem>(b =>
            {
                b.Property(x => x.Title).IsRequired().HasMaxLength(200);
                b.HasQueryFilter(e => e.TenantId == _tenantProvider.TenantId);
            });

            builder.Entity<Attachment>(b =>
            {
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
        }

        public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            var tenantIdCurrent = _tenantProvider.TenantId;
            foreach (var entry in ChangeTracker.Entries<ITenantOwned>())
            {
                if (entry.State == EntityState.Added && entry.Entity.TenantId == Guid.Empty)
                {
                    entry.Entity.TenantId = tenantIdCurrent;
                }
            }

            return base.SaveChangesAsync(cancellationToken);
        }
    }
}
