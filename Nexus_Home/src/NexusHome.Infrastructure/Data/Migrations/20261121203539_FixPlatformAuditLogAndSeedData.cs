using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NexusHome.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class FixPlatformAuditLogAndSeedData : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "platform_roles",
                keyColumn: "Id",
                keyValue: new Guid("00000000-0000-0000-0000-000000000004"));

            migrationBuilder.DropColumn(
                name: "DeletedAt",
                table: "platform_audit_logs");

            migrationBuilder.DropColumn(
                name: "UpdatedAt",
                table: "platform_audit_logs");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "DeletedAt",
                table: "platform_audit_logs",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "UpdatedAt",
                table: "platform_audit_logs",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTimeOffset(new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)));

            migrationBuilder.InsertData(
                table: "platform_roles",
                columns: new[] { "Id", "CreatedAt", "DeletedAt", "Description", "HierarchyLevel", "IsSystemRole", "Key", "Name", "UpdatedAt" },
                values: new object[] { new Guid("00000000-0000-0000-0000-000000000004"), new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, "Read-only access to users and homes for support", 30, true, "SupportAgent", "Support Agent", new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)) });
        }
    }
}
