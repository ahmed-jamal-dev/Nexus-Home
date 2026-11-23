using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NexusHome.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class FixInvitationInvitedByUserFK : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Fix invited_by_user_id FK to point to users (not memberships) and make nullable
            // This enables Claim Credential generation where TechnicianUserId (a User ID) is the provisioner

            // 1. Drop incorrect FK constraint to memberships
            migrationBuilder.Sql("ALTER TABLE invitations DROP CONSTRAINT IF EXISTS \"FK_invitations_memberships_invited_by_user_id\"");

            // 2. Clear existing invited_by_user_id values (they reference memberships, not users)
            //    Making the column nullable allows this; SET NULL avoids FK violation when adding new FK
            migrationBuilder.Sql("UPDATE invitations SET invited_by_user_id = NULL");

            // 3. Make column nullable (matches domain model: Guid?)
            migrationBuilder.AlterColumn<Guid>(
                name: "invited_by_user_id",
                table: "invitations",
                type: "uuid",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uuid");

            // 4. Add correct FK to users with SET NULL on delete (matches InvitationConfiguration)
            migrationBuilder.AddForeignKey(
                name: "FK_invitations_users_invited_by_user_id",
                table: "invitations",
                column: "invited_by_user_id",
                principalTable: "users",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Revert: restore incorrect FK to memberships (NOT NULL, CASCADE)
            migrationBuilder.DropForeignKey(
                name: "FK_invitations_users_invited_by_user_id",
                table: "invitations");

            migrationBuilder.AlterColumn<Guid>(
                name: "invited_by_user_id",
                table: "invitations",
                type: "uuid",
                nullable: false,
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);

            migrationBuilder.AddForeignKey(
                name: "FK_invitations_memberships_invited_by_user_id",
                table: "invitations",
                column: "invited_by_user_id",
                principalTable: "memberships",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
