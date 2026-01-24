namespace Crm.Infrastructure.Persistence.Migrations
{
    using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

    public partial class UpdateRefreshTokenIndexes : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("DROP INDEX IF EXISTS \"IX_RefreshTokens_UserId_TokenHash\";");

            migrationBuilder.Sql("CREATE UNIQUE INDEX IF NOT EXISTS \"IX_RefreshTokens_TokenHash\" ON \"RefreshTokens\" (\"TokenHash\");");

            migrationBuilder.Sql("CREATE INDEX IF NOT EXISTS \"IX_RefreshTokens_UserId_TenantId\" ON \"RefreshTokens\" (\"UserId\", \"TenantId\");");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("DROP INDEX IF EXISTS \"IX_RefreshTokens_TokenHash\";");
            migrationBuilder.Sql("DROP INDEX IF EXISTS \"IX_RefreshTokens_UserId_TenantId\";");

            migrationBuilder.Sql("CREATE UNIQUE INDEX IF NOT EXISTS \"IX_RefreshTokens_UserId_TokenHash\" ON \"RefreshTokens\" (\"UserId\", \"TokenHash\");");
        }
    }
}
