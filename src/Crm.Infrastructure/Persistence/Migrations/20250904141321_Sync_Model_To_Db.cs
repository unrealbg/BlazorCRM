namespace Crm.Infrastructure.Persistence.Migrations
{
    using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

    /// <inheritdoc />
    public partial class Sync_Model_To_Db : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"CREATE INDEX IF NOT EXISTS ix_tasks_tenantid_relatedid ON ""Tasks"" (""TenantId"", ""RelatedId"");");
            migrationBuilder.Sql(@"CREATE INDEX IF NOT EXISTS ix_deals_tenantid_companyid ON ""Deals"" (""TenantId"", ""CompanyId"");");
            migrationBuilder.Sql(@"CREATE INDEX IF NOT EXISTS ix_deals_tenantid_contactid ON ""Deals"" (""TenantId"", ""ContactId"");");
            migrationBuilder.Sql(@"CREATE INDEX IF NOT EXISTS ix_deals_tenantid_ownerid ON ""Deals"" (""TenantId"", ""OwnerId"");");
            migrationBuilder.Sql(@"CREATE INDEX IF NOT EXISTS ix_deals_tenantid_stageid ON ""Deals"" (""TenantId"", ""StageId"");");
            migrationBuilder.Sql(@"CREATE INDEX IF NOT EXISTS ix_attachments_tenantid_relatedto_relatedid ON ""Attachments"" (""TenantId"", ""RelatedTo"", ""RelatedId"");");
            migrationBuilder.Sql(@"CREATE INDEX IF NOT EXISTS ix_activities_tenantid_relatedid ON ""Activities"" (""TenantId"", ""RelatedId"");");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"DROP INDEX IF EXISTS ix_tasks_tenantid_relatedid;");
            migrationBuilder.Sql(@"DROP INDEX IF EXISTS ix_deals_tenantid_companyid;");
            migrationBuilder.Sql(@"DROP INDEX IF EXISTS ix_deals_tenantid_contactid;");
            migrationBuilder.Sql(@"DROP INDEX IF EXISTS ix_deals_tenantid_ownerid;");
            migrationBuilder.Sql(@"DROP INDEX IF EXISTS ix_deals_tenantid_stageid;");
            migrationBuilder.Sql(@"DROP INDEX IF EXISTS ix_attachments_tenantid_relatedto_relatedid;");
            migrationBuilder.Sql(@"DROP INDEX IF EXISTS ix_activities_tenantid_relatedid;");
        }
    }
}
