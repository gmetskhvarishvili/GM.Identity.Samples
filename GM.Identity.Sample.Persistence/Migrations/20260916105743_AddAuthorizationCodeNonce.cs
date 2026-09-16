using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GM.Identity.Sample.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddAuthorizationCodeNonce : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Nonce",
                schema: "application",
                table: "authorization_codes",
                type: "text",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Nonce",
                schema: "application",
                table: "authorization_codes");
        }
    }
}
