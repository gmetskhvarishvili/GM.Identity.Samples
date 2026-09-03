using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace GM.Identity.Sample.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class SeedNoneTwoFactorAuthType : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<int>(
                name: "Id",
                schema: "identity",
                table: "TwoFactorAuthTypes",
                type: "integer",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "integer")
                .OldAnnotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn);

            migrationBuilder.InsertData(
                schema: "identity",
                table: "TwoFactorAuthTypes",
                columns: new[] { "Id", "CreatedAt", "DisplayName", "IsActive", "Name", "UpdatedAt" },
                values: new object[] { 0, new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "None", true, "None", new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc) });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                schema: "identity",
                table: "TwoFactorAuthTypes",
                keyColumn: "Id",
                keyValue: 0);

            migrationBuilder.AlterColumn<int>(
                name: "Id",
                schema: "identity",
                table: "TwoFactorAuthTypes",
                type: "integer",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "integer")
                .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn);
        }
    }
}
