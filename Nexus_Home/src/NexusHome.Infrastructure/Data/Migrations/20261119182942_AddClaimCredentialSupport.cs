using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NexusHome.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddClaimCredentialSupport : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "uk_invitations_home_email_pending",
                table: "invitations");

            migrationBuilder.DropIndex(
                name: "uk_invitations_token",
                table: "invitations");

            migrationBuilder.AlterColumn<string>(
                name: "invitee_email",
                table: "invitations",
                type: "character varying(255)",
                maxLength: 255,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(255)",
                oldMaxLength: 255);

            migrationBuilder.CreateIndex(
                name: "idx_invitations_home_pending_claim",
                table: "invitations",
                column: "HomeId",
                filter: "is_accepted = false AND \"Role\" = 'owner' AND invitee_email IS NULL AND \"DeletedAt\" IS NULL");

            migrationBuilder.CreateIndex(
                name: "uk_invitations_home_email_pending",
                table: "invitations",
                columns: new[] { "HomeId", "invitee_email" },
                unique: true,
                filter: "is_accepted = false AND invitee_email IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "uk_invitations_token",
                table: "invitations",
                column: "Token",
                unique: true,
                filter: "\"Token\" <> ''");

            migrationBuilder.AddCheckConstraint(
                name: "chk_invitations_claim_credential",
                table: "invitations",
                sql: "(\"Role\" = 'owner' AND invitee_email IS NULL) OR (invitee_email IS NOT NULL)");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "idx_invitations_home_pending_claim",
                table: "invitations");

            migrationBuilder.DropIndex(
                name: "uk_invitations_home_email_pending",
                table: "invitations");

            migrationBuilder.DropIndex(
                name: "uk_invitations_token",
                table: "invitations");

            migrationBuilder.DropCheckConstraint(
                name: "chk_invitations_claim_credential",
                table: "invitations");

            migrationBuilder.AlterColumn<string>(
                name: "invitee_email",
                table: "invitations",
                type: "character varying(255)",
                maxLength: 255,
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "character varying(255)",
                oldMaxLength: 255,
                oldNullable: true);

            migrationBuilder.CreateIndex(
                name: "uk_invitations_home_email_pending",
                table: "invitations",
                columns: new[] { "HomeId", "invitee_email" },
                unique: true,
                filter: "is_accepted = false");

            migrationBuilder.CreateIndex(
                name: "uk_invitations_token",
                table: "invitations",
                column: "Token",
                unique: true);
        }
    }
}
