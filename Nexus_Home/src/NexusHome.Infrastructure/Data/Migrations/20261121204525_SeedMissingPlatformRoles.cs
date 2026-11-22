using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NexusHome.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class SeedMissingPlatformRoles : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            var seedTimestamp = new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0));

            migrationBuilder.InsertData(
                table: "platform_roles",
                columns: new[] { "Id", "CreatedAt", "DeletedAt", "Description", "HierarchyLevel", "IsSystemRole", "Key", "Name", "UpdatedAt" },
                values: new object[,]
                {
                    {
                        new Guid("00000000-0000-0000-0000-000000000001"),
                        seedTimestamp,
                        null,
                        "Full platform access including role management and user administration",
                        100,
                        true,
                        "SuperAdmin",
                        "Super Administrator",
                        seedTimestamp
                    },
                    {
                        new Guid("00000000-0000-0000-0000-000000000002"),
                        seedTimestamp,
                        null,
                        "Platform administration including user management and home oversight",
                        80,
                        true,
                        "PlatformAdmin",
                        "Platform Administrator",
                        seedTimestamp
                    },
                    {
                        new Guid("00000000-0000-0000-0000-000000000003"),
                        seedTimestamp,
                        null,
                        "Provisions and manages assigned homes only",
                        50,
                        true,
                        "Technician",
                        "Technician",
                        seedTimestamp
                    }
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "platform_roles",
                keyColumn: "Id",
                keyValues: new object[]
                {
                    new Guid("00000000-0000-0000-0000-000000000001"),
                    new Guid("00000000-0000-0000-0000-000000000002"),
                    new Guid("00000000-0000-0000-0000-000000000003")
                });
        }
    }
}