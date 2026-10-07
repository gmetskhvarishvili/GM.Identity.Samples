using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GM.Identity.Sample.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class ConsolidateTotpOntoGmOtp : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Backfill any pre-existing enrolments with the migration time rather than year 0001; new rows set it
            // in the domain constructor.
            migrationBuilder.AddColumn<DateTime>(
                name: "CreatedAtUtc",
                schema: "application",
                table: "user_totp_devices",
                type: "timestamp with time zone",
                nullable: false,
                defaultValueSql: "now()");

            migrationBuilder.AddColumn<string>(
                name: "Subject",
                schema: "application",
                table: "user_totp_devices",
                type: "character varying(256)",
                maxLength: 256,
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CreatedAtUtc",
                schema: "application",
                table: "user_totp_devices");

            migrationBuilder.DropColumn(
                name: "Subject",
                schema: "application",
                table: "user_totp_devices");
        }
    }
}
