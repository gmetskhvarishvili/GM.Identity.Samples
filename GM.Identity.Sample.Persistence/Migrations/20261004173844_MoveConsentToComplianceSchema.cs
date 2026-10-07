using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GM.Identity.Sample.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class MoveConsentToComplianceSchema : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "compliance");

            migrationBuilder.RenameTable(
                name: "user_consents",
                schema: "application",
                newName: "user_consents",
                newSchema: "compliance");

            migrationBuilder.RenameTable(
                name: "consent_documents",
                schema: "application",
                newName: "consent_documents",
                newSchema: "compliance");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameTable(
                name: "user_consents",
                schema: "compliance",
                newName: "user_consents",
                newSchema: "application");

            migrationBuilder.RenameTable(
                name: "consent_documents",
                schema: "compliance",
                newName: "consent_documents",
                newSchema: "application");
        }
    }
}
