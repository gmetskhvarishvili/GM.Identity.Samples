using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GM.Identity.Sample.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class VersionConsentDocuments : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_consent_documents_ConsentType",
                schema: "application",
                table: "consent_documents");

            migrationBuilder.RenameColumn(
                name: "CurrentVersion",
                schema: "application",
                table: "consent_documents",
                newName: "Version");

            migrationBuilder.AddColumn<bool>(
                name: "IsCurrent",
                schema: "application",
                table: "consent_documents",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateIndex(
                name: "IX_consent_documents_ConsentType_IsCurrent",
                schema: "application",
                table: "consent_documents",
                columns: new[] { "ConsentType", "IsCurrent" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_consent_documents_ConsentType_IsCurrent",
                schema: "application",
                table: "consent_documents");

            migrationBuilder.DropColumn(
                name: "IsCurrent",
                schema: "application",
                table: "consent_documents");

            migrationBuilder.RenameColumn(
                name: "Version",
                schema: "application",
                table: "consent_documents",
                newName: "CurrentVersion");

            migrationBuilder.CreateIndex(
                name: "IX_consent_documents_ConsentType",
                schema: "application",
                table: "consent_documents",
                column: "ConsentType");
        }
    }
}
