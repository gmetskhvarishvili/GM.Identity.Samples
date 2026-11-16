using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GM.Identity.Sample.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class UpgradeGMMessagingTo2 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "dead_lettered_at_utc",
                schema: "messaging",
                table: "outbox_messages",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "locked_by",
                schema: "messaging",
                table: "outbox_messages",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "locked_until_utc",
                schema: "messaging",
                table: "outbox_messages",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "next_attempt_at_utc",
                schema: "messaging",
                table: "outbox_messages",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "trace_parent",
                schema: "messaging",
                table: "outbox_messages",
                type: "character varying(128)",
                maxLength: 128,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "ix_outbox_messages_pending",
                schema: "messaging",
                table: "outbox_messages",
                columns: new[] { "processed_at_utc", "dead_lettered_at_utc", "created_at_utc" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_outbox_messages_pending",
                schema: "messaging",
                table: "outbox_messages");

            migrationBuilder.DropColumn(
                name: "dead_lettered_at_utc",
                schema: "messaging",
                table: "outbox_messages");

            migrationBuilder.DropColumn(
                name: "locked_by",
                schema: "messaging",
                table: "outbox_messages");

            migrationBuilder.DropColumn(
                name: "locked_until_utc",
                schema: "messaging",
                table: "outbox_messages");

            migrationBuilder.DropColumn(
                name: "next_attempt_at_utc",
                schema: "messaging",
                table: "outbox_messages");

            migrationBuilder.DropColumn(
                name: "trace_parent",
                schema: "messaging",
                table: "outbox_messages");
        }
    }
}
