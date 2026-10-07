using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GM.Identity.Sample.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class RemoveTwoFactorConfirmation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ConfirmedAt",
                schema: "identity",
                table: "UserTwoFactorAuthTypes");

            migrationBuilder.DropColumn(
                name: "IsConfirmed",
                schema: "identity",
                table: "UserTwoFactorAuthTypes");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
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
        }
    }
}
