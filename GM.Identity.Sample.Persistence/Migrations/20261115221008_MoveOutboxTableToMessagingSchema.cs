using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GM.Identity.Sample.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class MoveOutboxTableToMessagingSchema : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "messaging");

            migrationBuilder.RenameTable(
                name: "outbox_messages",
                schema: "outbox_messages",
                newName: "outbox_messages",
                newSchema: "messaging");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "outbox_messages");

            migrationBuilder.RenameTable(
                name: "outbox_messages",
                schema: "messaging",
                newName: "outbox_messages",
                newSchema: "outbox_messages");
        }
    }
}
