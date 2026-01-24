namespace Crm.Infrastructure.Persistence.Migrations
{
    using System;
    using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

    public partial class AddCreatedAtUtcAndIndexes : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "CreatedAtUtc",
                table: "Companies",
                type: "timestamp with time zone",
                nullable: false,
                defaultValueSql: "now()");

            migrationBuilder.AddColumn<DateTime>(
                name: "CreatedAtUtc",
                table: "Contacts",
                type: "timestamp with time zone",
                nullable: false,
                defaultValueSql: "now()");

            migrationBuilder.AddColumn<DateTime>(
                name: "CreatedAtUtc",
                table: "Deals",
                type: "timestamp with time zone",
                nullable: false,
                defaultValueSql: "now()");

            migrationBuilder.AddColumn<DateTime>(
                name: "CreatedAtUtc",
                table: "Activities",
                type: "timestamp with time zone",
                nullable: false,
                defaultValueSql: "now()");

            migrationBuilder.AddColumn<DateTime>(
                name: "CreatedAtUtc",
                table: "Tasks",
                type: "timestamp with time zone",
                nullable: false,
                defaultValueSql: "now()");

            migrationBuilder.CreateIndex(
                name: "IX_Companies_TenantId_CreatedAtUtc",
                table: "Companies",
                columns: new[] { "TenantId", "CreatedAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_Companies_TenantId_Name",
                table: "Companies",
                columns: new[] { "TenantId", "Name" });

            migrationBuilder.CreateIndex(
                name: "IX_Contacts_TenantId_CreatedAtUtc",
                table: "Contacts",
                columns: new[] { "TenantId", "CreatedAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_Contacts_TenantId_Email",
                table: "Contacts",
                columns: new[] { "TenantId", "Email" });

            migrationBuilder.CreateIndex(
                name: "IX_Deals_TenantId_CreatedAtUtc",
                table: "Deals",
                columns: new[] { "TenantId", "CreatedAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_Activities_TenantId_CreatedAtUtc",
                table: "Activities",
                columns: new[] { "TenantId", "CreatedAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_Tasks_TenantId_CreatedAtUtc",
                table: "Tasks",
                columns: new[] { "TenantId", "CreatedAtUtc" });
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Companies_TenantId_CreatedAtUtc",
                table: "Companies");

            migrationBuilder.DropIndex(
                name: "IX_Companies_TenantId_Name",
                table: "Companies");

            migrationBuilder.DropIndex(
                name: "IX_Contacts_TenantId_CreatedAtUtc",
                table: "Contacts");

            migrationBuilder.DropIndex(
                name: "IX_Contacts_TenantId_Email",
                table: "Contacts");

            migrationBuilder.DropIndex(
                name: "IX_Deals_TenantId_CreatedAtUtc",
                table: "Deals");

            migrationBuilder.DropIndex(
                name: "IX_Activities_TenantId_CreatedAtUtc",
                table: "Activities");

            migrationBuilder.DropIndex(
                name: "IX_Tasks_TenantId_CreatedAtUtc",
                table: "Tasks");

            migrationBuilder.DropColumn(
                name: "CreatedAtUtc",
                table: "Companies");

            migrationBuilder.DropColumn(
                name: "CreatedAtUtc",
                table: "Contacts");

            migrationBuilder.DropColumn(
                name: "CreatedAtUtc",
                table: "Deals");

            migrationBuilder.DropColumn(
                name: "CreatedAtUtc",
                table: "Activities");

            migrationBuilder.DropColumn(
                name: "CreatedAtUtc",
                table: "Tasks");
        }
    }
}
