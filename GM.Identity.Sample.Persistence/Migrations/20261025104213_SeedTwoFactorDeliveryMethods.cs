using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace GM.Identity.Sample.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class SeedTwoFactorDeliveryMethods : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.InsertData(
                schema: "identity",
                table: "two_factor_auth_types",
                columns: new[] { "id", "created_at", "created_by", "display_name", "is_active", "name", "updated_at", "updated_by" },
                values: new object[,]
                {
                    { 1, new DateTime(2026, 10, 25, 0, 0, 0, 0, DateTimeKind.Utc), null, "Email", true, "Email", new DateTime(2026, 10, 25, 0, 0, 0, 0, DateTimeKind.Utc), null },
                    { 2, new DateTime(2026, 10, 25, 0, 0, 0, 0, DateTimeKind.Utc), null, "SMS", true, "Sms", new DateTime(2026, 10, 25, 0, 0, 0, 0, DateTimeKind.Utc), null },
                    { 3, new DateTime(2026, 10, 25, 0, 0, 0, 0, DateTimeKind.Utc), null, "WhatsApp", true, "WhatsApp", new DateTime(2026, 10, 25, 0, 0, 0, 0, DateTimeKind.Utc), null }
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                schema: "identity",
                table: "two_factor_auth_types",
                keyColumn: "id",
                keyValue: 1);

            migrationBuilder.DeleteData(
                schema: "identity",
                table: "two_factor_auth_types",
                keyColumn: "id",
                keyValue: 2);

            migrationBuilder.DeleteData(
                schema: "identity",
                table: "two_factor_auth_types",
                keyColumn: "id",
                keyValue: 3);
        }
    }
}
