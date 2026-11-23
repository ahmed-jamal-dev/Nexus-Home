using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NexusHome.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddUniqueClaimCredentialIndex : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "idx_invitations_home_pending_claim",
                table: "invitations");

            migrationBuilder.CreateIndex(
                name: "uk_invitations_home_pending_claim_credential",
                table: "invitations",
                column: "HomeId",
                unique: true,
                filter: "is_accepted = false AND \"Role\" = 'Owner' AND invitee_email IS NULL AND deleted_at IS NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "uk_invitations_home_pending_claim_credential",
                table: "invitations");

            migrationBuilder.CreateIndex(
                name: "idx_invitations_home_pending_claim",
                table: "invitations",
                column: "HomeId",
                filter: "is_accepted = false AND \"Role\" = 'Owner' AND invitee_email IS NULL AND deleted_at IS NULL");
        }
    }
}
