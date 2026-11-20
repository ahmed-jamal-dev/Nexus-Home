using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NexusHome.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class FixClaimCredentialIndexFilter : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "idx_invitations_home_pending_claim",
                table: "invitations");

            migrationBuilder.CreateIndex(
                name: "idx_invitations_home_pending_claim",
                table: "invitations",
                column: "HomeId",
                filter: "is_accepted = false AND \"Role\" = 'Owner' AND invitee_email IS NULL AND \"DeletedAt\" IS NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "idx_invitations_home_pending_claim",
                table: "invitations");

            migrationBuilder.CreateIndex(
                name: "idx_invitations_home_pending_claim",
                table: "invitations",
                column: "HomeId",
                filter: "is_accepted = false AND \"Role\" = 'owner' AND invitee_email IS NULL AND \"DeletedAt\" IS NULL");
        }
    }
}
