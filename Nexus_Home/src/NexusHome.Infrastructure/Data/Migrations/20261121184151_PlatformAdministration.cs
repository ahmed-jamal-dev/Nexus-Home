using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace NexusHome.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class PlatformAdministration : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "platform_roles_version",
                table: "users",
                type: "integer",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.AddColumn<Guid>(
                name: "provisioned_by_technician_id",
                table: "homes",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "platform_audit_logs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ActorUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    Action = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    TargetType = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    TargetId = table.Column<Guid>(type: "uuid", nullable: false),
                    Details = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: true),
                    IpAddress = table.Column<string>(type: "character varying(45)", maxLength: 45, nullable: true),
                    UserAgent = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    Success = table.Column<bool>(type: "boolean", nullable: false),
                    ErrorMessage = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    DeletedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_platform_audit_logs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_platform_audit_logs_users_ActorUserId",
                        column: x => x.ActorUserId,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "platform_roles",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Key = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    HierarchyLevel = table.Column<int>(type: "integer", nullable: false),
                    IsSystemRole = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    DeletedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_platform_roles", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "user_platform_roles",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    PlatformRoleId = table.Column<Guid>(type: "uuid", nullable: false),
                    ScopeId = table.Column<Guid>(type: "uuid", nullable: true),
                    ScopeType = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    AssignedBy = table.Column<Guid>(type: "uuid", nullable: false),
                    AssignedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    ExpiresAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    DeletedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_user_platform_roles", x => x.Id);
                    table.ForeignKey(
                        name: "FK_user_platform_roles_platform_roles_PlatformRoleId",
                        column: x => x.PlatformRoleId,
                        principalTable: "platform_roles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_user_platform_roles_users_UserId",
                        column: x => x.UserId,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.InsertData(
                table: "platform_roles",
                columns: new[] { "Id", "CreatedAt", "DeletedAt", "Description", "HierarchyLevel", "IsSystemRole", "Key", "Name", "UpdatedAt" },
                values: new object[,]
                {
                    { new Guid("00000000-0000-0000-0000-000000000001"), new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, "Full platform access including role management and user administration", 100, true, "SuperAdmin", "Super Administrator", new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)) },
                    { new Guid("00000000-0000-0000-0000-000000000002"), new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, "Platform administration including user management and home oversight", 80, true, "PlatformAdmin", "Platform Administrator", new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)) },
                    { new Guid("00000000-0000-0000-0000-000000000003"), new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, "Provisions and manages assigned homes only", 50, true, "Technician", "Technician", new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)) },
                    { new Guid("00000000-0000-0000-0000-000000000004"), new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, "Read-only access to users and homes for support", 30, true, "SupportAgent", "Support Agent", new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)) }
                });

            migrationBuilder.CreateIndex(
                name: "idx_homes_provisioned_by_technician",
                table: "homes",
                column: "provisioned_by_technician_id");

            migrationBuilder.CreateIndex(
                name: "ix_platform_audit_logs_actor_user_id",
                table: "platform_audit_logs",
                column: "ActorUserId");

            migrationBuilder.CreateIndex(
                name: "ix_platform_audit_logs_created_at",
                table: "platform_audit_logs",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "ix_platform_audit_logs_target",
                table: "platform_audit_logs",
                columns: new[] { "TargetType", "TargetId" });

            migrationBuilder.CreateIndex(
                name: "ix_platform_audit_logs_target_id",
                table: "platform_audit_logs",
                column: "TargetId");

            migrationBuilder.CreateIndex(
                name: "ix_platform_audit_logs_target_type",
                table: "platform_audit_logs",
                column: "TargetType");

            migrationBuilder.CreateIndex(
                name: "ix_platform_roles_key",
                table: "platform_roles",
                column: "Key",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_user_platform_roles_role_id",
                table: "user_platform_roles",
                column: "PlatformRoleId");

            migrationBuilder.CreateIndex(
                name: "ix_user_platform_roles_scope",
                table: "user_platform_roles",
                columns: new[] { "ScopeType", "ScopeId" });

            migrationBuilder.CreateIndex(
                name: "ix_user_platform_roles_unique",
                table: "user_platform_roles",
                columns: new[] { "UserId", "PlatformRoleId", "ScopeType", "ScopeId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_user_platform_roles_user_id",
                table: "user_platform_roles",
                column: "UserId");

            migrationBuilder.AddForeignKey(
                name: "FK_homes_users_provisioned_by_technician_id",
                table: "homes",
                column: "provisioned_by_technician_id",
                principalTable: "users",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_homes_users_provisioned_by_technician_id",
                table: "homes");

            migrationBuilder.DropTable(
                name: "platform_audit_logs");

            migrationBuilder.DropTable(
                name: "user_platform_roles");

            migrationBuilder.DropTable(
                name: "platform_roles");

            migrationBuilder.DropIndex(
                name: "idx_homes_provisioned_by_technician",
                table: "homes");

            migrationBuilder.DropColumn(
                name: "platform_roles_version",
                table: "users");

            migrationBuilder.DropColumn(
                name: "provisioned_by_technician_id",
                table: "homes");
        }
    }
}
