using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Crm.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class Sync_Model_To_Db : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_Tasks_TenantId_RelatedId",
                table: "Tasks",
                columns: new[] { "TenantId", "RelatedId" });

            migrationBuilder.CreateIndex(
                name: "IX_Deals_TenantId_CompanyId",
                table: "Deals",
                columns: new[] { "TenantId", "CompanyId" });

            migrationBuilder.CreateIndex(
                name: "IX_Deals_TenantId_ContactId",
                table: "Deals",
                columns: new[] { "TenantId", "ContactId" });

            migrationBuilder.CreateIndex(
                name: "IX_Deals_TenantId_OwnerId",
                table: "Deals",
                columns: new[] { "TenantId", "OwnerId" });

            migrationBuilder.CreateIndex(
                name: "IX_Deals_TenantId_StageId",
                table: "Deals",
                columns: new[] { "TenantId", "StageId" });

            migrationBuilder.CreateIndex(
                name: "IX_Attachments_TenantId_RelatedTo_RelatedId",
                table: "Attachments",
                columns: new[] { "TenantId", "RelatedTo", "RelatedId" });

            migrationBuilder.CreateIndex(
                name: "IX_Activities_TenantId_RelatedId",
                table: "Activities",
                columns: new[] { "TenantId", "RelatedId" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Tasks_TenantId_RelatedId",
                table: "Tasks");

            migrationBuilder.DropIndex(
                name: "IX_Deals_TenantId_CompanyId",
                table: "Deals");

            migrationBuilder.DropIndex(
                name: "IX_Deals_TenantId_ContactId",
                table: "Deals");

            migrationBuilder.DropIndex(
                name: "IX_Deals_TenantId_OwnerId",
                table: "Deals");

            migrationBuilder.DropIndex(
                name: "IX_Deals_TenantId_StageId",
                table: "Deals");

            migrationBuilder.DropIndex(
                name: "IX_Attachments_TenantId_RelatedTo_RelatedId",
                table: "Attachments");

            migrationBuilder.DropIndex(
                name: "IX_Activities_TenantId_RelatedId",
                table: "Activities");
        }
    }
}
