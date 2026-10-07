using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GM.Identity.Sample.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddUserSessionActorContext : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ChannelId",
                schema: "authorization",
                table: "UserSessions",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CorrelationId",
                schema: "authorization",
                table: "UserSessions",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Culture",
                schema: "authorization",
                table: "UserSessions",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "IdempotencyKey",
                schema: "authorization",
                table: "UserSessions",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "IpAddress",
                schema: "authorization",
                table: "UserSessions",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Source",
                schema: "authorization",
                table: "UserSessions",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "TenantId",
                schema: "authorization",
                table: "UserSessions",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "UserAgent",
                schema: "authorization",
                table: "UserSessions",
                type: "text",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ChannelId",
                schema: "authorization",
                table: "UserSessions");

            migrationBuilder.DropColumn(
                name: "CorrelationId",
                schema: "authorization",
                table: "UserSessions");

            migrationBuilder.DropColumn(
                name: "Culture",
                schema: "authorization",
                table: "UserSessions");

            migrationBuilder.DropColumn(
                name: "IdempotencyKey",
                schema: "authorization",
                table: "UserSessions");

            migrationBuilder.DropColumn(
                name: "IpAddress",
                schema: "authorization",
                table: "UserSessions");

            migrationBuilder.DropColumn(
                name: "Source",
                schema: "authorization",
                table: "UserSessions");

            migrationBuilder.DropColumn(
                name: "TenantId",
                schema: "authorization",
                table: "UserSessions");

            migrationBuilder.DropColumn(
                name: "UserAgent",
                schema: "authorization",
                table: "UserSessions");
        }
    }
}
