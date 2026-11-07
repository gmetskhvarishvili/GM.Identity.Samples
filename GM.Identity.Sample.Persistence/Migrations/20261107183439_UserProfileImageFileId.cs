using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GM.Identity.Sample.Persistence.Migrations
{
    /// <summary>
    /// The user's profile image becomes a file id (GM.Identity 4.0.0): profile_image_key (a storage key, up to 512
    /// characters) turns into profile_image_file_id (uuid). Keys that are already a GUID carry over; any other key
    /// (a storage path) can't name a file id, so it's cleared.
    /// </summary>
    public partial class UserProfileImageFileId : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "profile_image_key",
                schema: "identity",
                table: "users",
                newName: "profile_image_file_id");

            migrationBuilder.Sql(
                """
                ALTER TABLE identity.users
                    ALTER COLUMN profile_image_file_id TYPE uuid
                    USING CASE
                        WHEN profile_image_file_id ~* '^[0-9a-f]{8}-?[0-9a-f]{4}-?[0-9a-f]{4}-?[0-9a-f]{4}-?[0-9a-f]{12}$'
                            THEN profile_image_file_id::uuid
                    END;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                ALTER TABLE identity.users
                    ALTER COLUMN profile_image_file_id TYPE character varying(512)
                    USING profile_image_file_id::text;
                """);

            migrationBuilder.RenameColumn(
                name: "profile_image_file_id",
                schema: "identity",
                table: "users",
                newName: "profile_image_key");
        }
    }
}
