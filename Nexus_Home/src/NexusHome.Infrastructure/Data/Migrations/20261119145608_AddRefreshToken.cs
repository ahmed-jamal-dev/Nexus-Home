using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NexusHome.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddRefreshToken : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ai_action_audit_device_commands_DeviceCommandId",
                table: "ai_action_audit");

            migrationBuilder.DropIndex(
                name: "idx_rooms_home_id",
                table: "rooms");

            migrationBuilder.DropIndex(
                name: "idx_permissions_grantee_membership_id",
                table: "permissions");

            migrationBuilder.DropIndex(
                name: "idx_memberships_user_id",
                table: "memberships");

            migrationBuilder.DropIndex(
                name: "idx_invitations_home_id",
                table: "invitations");

            migrationBuilder.DropIndex(
                name: "idx_devices_home_id",
                table: "devices");

            migrationBuilder.DropIndex(
                name: "idx_device_commands_device_id",
                table: "device_commands");

            migrationBuilder.DropIndex(
                name: "idx_device_commands_requested_by_user_id",
                table: "device_commands");

            migrationBuilder.DropIndex(
                name: "IX_ai_action_audit_DeviceCommandId",
                table: "ai_action_audit");

            migrationBuilder.DropColumn(
                name: "DeviceCommandId",
                table: "ai_action_audit");

            migrationBuilder.CreateTable(
                name: "refresh_tokens",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    token_hash = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    expires_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    revoked_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    replaced_by_token_hash = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    revocation_reason = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    DeletedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_refresh_tokens", x => x.Id);
                    table.ForeignKey(
                        name: "FK_refresh_tokens_users_user_id",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ai_action_audit_command_id",
                table: "ai_action_audit",
                column: "command_id");

            migrationBuilder.CreateIndex(
                name: "idx_refresh_tokens_expires_at",
                table: "refresh_tokens",
                column: "expires_at",
                filter: "revoked_at IS NULL");

            migrationBuilder.CreateIndex(
                name: "idx_refresh_tokens_user_hash_active",
                table: "refresh_tokens",
                columns: new[] { "user_id", "token_hash" },
                filter: "revoked_at IS NULL");

            migrationBuilder.CreateIndex(
                name: "idx_refresh_tokens_user_id",
                table: "refresh_tokens",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "uk_refresh_tokens_token_hash",
                table: "refresh_tokens",
                column: "token_hash",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_ai_action_audit_device_commands_command_id",
                table: "ai_action_audit",
                column: "command_id",
                principalTable: "device_commands",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ai_action_audit_device_commands_command_id",
                table: "ai_action_audit");

            migrationBuilder.DropTable(
                name: "refresh_tokens");

            migrationBuilder.DropIndex(
                name: "IX_ai_action_audit_command_id",
                table: "ai_action_audit");

            migrationBuilder.AddColumn<Guid>(
                name: "DeviceCommandId",
                table: "ai_action_audit",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "idx_rooms_home_id",
                table: "rooms",
                column: "HomeId");

            migrationBuilder.CreateIndex(
                name: "idx_permissions_grantee_membership_id",
                table: "permissions",
                column: "grantee_membership_id");

            migrationBuilder.CreateIndex(
                name: "idx_memberships_user_id",
                table: "memberships",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "idx_invitations_home_id",
                table: "invitations",
                column: "HomeId");

            migrationBuilder.CreateIndex(
                name: "idx_devices_home_id",
                table: "devices",
                column: "HomeId");

            migrationBuilder.CreateIndex(
                name: "idx_device_commands_device_id",
                table: "device_commands",
                column: "device_id");

            migrationBuilder.CreateIndex(
                name: "idx_device_commands_requested_by_user_id",
                table: "device_commands",
                column: "requested_by_user_id");

            migrationBuilder.CreateIndex(
                name: "IX_ai_action_audit_DeviceCommandId",
                table: "ai_action_audit",
                column: "DeviceCommandId");

            migrationBuilder.AddForeignKey(
                name: "FK_ai_action_audit_device_commands_DeviceCommandId",
                table: "ai_action_audit",
                column: "DeviceCommandId",
                principalTable: "device_commands",
                principalColumn: "Id");
        }
    }
}
