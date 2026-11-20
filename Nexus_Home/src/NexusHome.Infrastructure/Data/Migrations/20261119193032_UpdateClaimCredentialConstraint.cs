using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NexusHome.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class UpdateClaimCredentialConstraint : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "chk_invitations_claim_credential",
                table: "invitations");

            migrationBuilder.AddCheckConstraint(
                name: "chk_invitations_claim_credential",
                table: "invitations",
                sql: "(\"Role\" = 'Owner' AND invitee_email IS NULL) OR (invitee_email IS NOT NULL)");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "chk_invitations_claim_credential",
                table: "invitations");

            migrationBuilder.AddCheckConstraint(
                name: "chk_invitations_claim_credential",
                table: "invitations",
                sql: "(\"Role\" = 'owner' AND invitee_email IS NULL) OR (invitee_email IS NOT NULL)");
        }
    }
}
