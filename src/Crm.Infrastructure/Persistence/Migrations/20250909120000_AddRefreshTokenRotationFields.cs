namespace Crm.Infrastructure.Persistence.Migrations
{
    using System;
    using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

    public partial class AddRefreshTokenRotationFields : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"CREATE TABLE IF NOT EXISTS ""RefreshTokens"" (
    ""Id"" uuid PRIMARY KEY,
    ""UserId"" text NOT NULL,
    ""TenantId"" uuid NOT NULL DEFAULT '00000000-0000-0000-0000-000000000000',
    ""TokenHash"" text NOT NULL,
    ""ExpiresAtUtc"" timestamp with time zone NOT NULL,
    ""IsRevoked"" boolean NOT NULL DEFAULT false,
    ""ReplacedByHash"" text NULL,
    ""CreatedAtUtc"" timestamp with time zone NOT NULL,
    ""RevokedAtUtc"" timestamp with time zone NULL,
    ""CreatedByIp"" text NULL,
    ""RevokedByIp"" text NULL,
    ""UserAgent"" text NULL
);");

            migrationBuilder.Sql(@"ALTER TABLE ""RefreshTokens"" ADD COLUMN IF NOT EXISTS ""TenantId"" uuid NOT NULL DEFAULT '00000000-0000-0000-0000-000000000000';");
            migrationBuilder.Sql(@"ALTER TABLE ""RefreshTokens"" ADD COLUMN IF NOT EXISTS ""CreatedByIp"" text NULL;");
            migrationBuilder.Sql(@"ALTER TABLE ""RefreshTokens"" ADD COLUMN IF NOT EXISTS ""RevokedAtUtc"" timestamp with time zone NULL;");
            migrationBuilder.Sql(@"ALTER TABLE ""RefreshTokens"" ADD COLUMN IF NOT EXISTS ""RevokedByIp"" text NULL;");
            migrationBuilder.Sql(@"ALTER TABLE ""RefreshTokens"" ADD COLUMN IF NOT EXISTS ""UserAgent"" text NULL;");

            migrationBuilder.Sql(@"CREATE INDEX IF NOT EXISTS ix_refreshtokens_user_tenant_revoked ON ""RefreshTokens""(""UserId"",""TenantId"",""IsRevoked"");");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"DROP INDEX IF EXISTS ix_refreshtokens_user_tenant_revoked;");
            migrationBuilder.Sql(@"ALTER TABLE ""RefreshTokens"" DROP COLUMN IF EXISTS ""TenantId"";");
            migrationBuilder.Sql(@"ALTER TABLE ""RefreshTokens"" DROP COLUMN IF EXISTS ""CreatedByIp"";");
            migrationBuilder.Sql(@"ALTER TABLE ""RefreshTokens"" DROP COLUMN IF EXISTS ""RevokedAtUtc"";");
            migrationBuilder.Sql(@"ALTER TABLE ""RefreshTokens"" DROP COLUMN IF EXISTS ""RevokedByIp"";");
            migrationBuilder.Sql(@"ALTER TABLE ""RefreshTokens"" DROP COLUMN IF EXISTS ""UserAgent"";");
        }
    }
}
