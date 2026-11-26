using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NexusHome.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class FixPermissionColumnTypes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Convert enum columns to text
            migrationBuilder.Sql(@"
                ALTER TABLE ""permissions""
                ALTER COLUMN ""permission_type"" TYPE text USING ""permission_type""::text,
                ALTER COLUMN ""target_type"" TYPE text USING ""target_type""::text,
                ALTER COLUMN ""effect"" TYPE text USING ""effect""::text;
            ");

            // Drop the enum types (no longer needed)
            migrationBuilder.Sql(@"
                DROP TYPE IF EXISTS permission_type_enum;
                DROP TYPE IF EXISTS target_type_enum;
                DROP TYPE IF EXISTS effect_enum;
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Recreate enum types
            migrationBuilder.Sql(@"
                CREATE TYPE permission_type_enum AS ENUM (
                    'device:control', 'device:view', 'device:manage',
                    'home:manage', 'member:manage', 'camera:stream',
                    'camera:view', 'microphone:stream', 'settings:manage',
                    'ai:propose', 'ai:execute'
                );
                CREATE TYPE target_type_enum AS ENUM (
                    'device', 'home', 'room', 'camera', 'microphone'
                );
                CREATE TYPE effect_enum AS ENUM ('Grant', 'Deny');
            ");

            // Convert columns back to enum types
            migrationBuilder.Sql(@"
                ALTER TABLE ""permissions""
                ALTER COLUMN ""permission_type"" TYPE permission_type_enum USING ""permission_type""::permission_type_enum,
                ALTER COLUMN ""target_type"" TYPE target_type_enum USING ""target_type""::target_type_enum,
                ALTER COLUMN ""effect"" TYPE effect_enum USING ""effect""::effect_enum;
            ");
        }
    }
}
