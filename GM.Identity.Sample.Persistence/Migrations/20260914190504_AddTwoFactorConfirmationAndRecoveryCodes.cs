using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GM.Identity.Sample.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddTwoFactorConfirmationAndRecoveryCodes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "ConfirmedAt",
                schema: "identity",
                table: "UserTwoFactorAuthTypes",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsConfirmed",
                schema: "identity",
                table: "UserTwoFactorAuthTypes",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            // Existing enrolments predate the pending/confirmed distinction and already gate login — treat
            // them as confirmed so this migration never locks an already-enrolled user out.
            migrationBuilder.Sql(
                "UPDATE identity.\"UserTwoFactorAuthTypes\" " +
                "SET \"IsConfirmed\" = TRUE, \"ConfirmedAt\" = NOW() WHERE \"IsConfirmed\" = FALSE;");

            migrationBuilder.CreateTable(
                name: "user_recovery_codes",
                schema: "application",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    CodeHash = table.Column<string>(type: "text", nullable: false),
                    UsedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    UpdatedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    IsHidden = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_user_recovery_codes", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_user_recovery_codes_UserId_CodeHash",
                schema: "application",
                table: "user_recovery_codes",
                columns: new[] { "UserId", "CodeHash" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "user_recovery_codes",
                schema: "application");

            migrationBuilder.DropColumn(
                name: "ConfirmedAt",
                schema: "identity",
                table: "UserTwoFactorAuthTypes");

            migrationBuilder.DropColumn(
                name: "IsConfirmed",
                schema: "identity",
                table: "UserTwoFactorAuthTypes");
        }
    }
}
