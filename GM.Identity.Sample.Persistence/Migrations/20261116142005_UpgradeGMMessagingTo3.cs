using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GM.Identity.Sample.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class UpgradeGMMessagingTo3 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "event_version",
                schema: "messaging",
                table: "outbox_messages",
                type: "integer",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.AddColumn<string>(
                name: "ordering_key",
                schema: "messaging",
                table: "outbox_messages",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "ix_outbox_messages_ordering",
                schema: "messaging",
                table: "outbox_messages",
                columns: new[] { "ordering_key", "created_at_utc" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_outbox_messages_ordering",
                schema: "messaging",
                table: "outbox_messages");

            migrationBuilder.DropColumn(
                name: "event_version",
                schema: "messaging",
                table: "outbox_messages");

            migrationBuilder.DropColumn(
                name: "ordering_key",
                schema: "messaging",
                table: "outbox_messages");
        }
    }
}
