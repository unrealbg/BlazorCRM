namespace Crm.Infrastructure.Persistence.Migrations
{
    using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

    public partial class FixAuditEntriesConcurrencyStamp : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
DO $$
BEGIN
    IF EXISTS (
        SELECT 1
        FROM information_schema.columns
        WHERE table_schema = 'public'
          AND table_name = 'AuditEntries'
          AND column_name = 'ConcurrencyStamp'
    ) THEN
        ALTER TABLE ""AuditEntries"" ALTER COLUMN ""ConcurrencyStamp"" DROP NOT NULL;
    END IF;
END $$;
");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // no-op
        }
    }
}
