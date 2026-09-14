using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GM.Identity.Sample.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddEventSourceAndActorContext : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ChannelId",
                schema: "domainEvents",
                table: "DomainEvents",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "ClientId",
                schema: "domainEvents",
                table: "DomainEvents",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Culture",
                schema: "domainEvents",
                table: "DomainEvents",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Source",
                schema: "domainEvents",
                table: "DomainEvents",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "TenantId",
                schema: "domainEvents",
                table: "DomainEvents",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_DomainEvents_TenantId",
                schema: "domainEvents",
                table: "DomainEvents",
                column: "TenantId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_DomainEvents_TenantId",
                schema: "domainEvents",
                table: "DomainEvents");

            migrationBuilder.DropColumn(
                name: "ChannelId",
                schema: "domainEvents",
                table: "DomainEvents");

            migrationBuilder.DropColumn(
                name: "ClientId",
                schema: "domainEvents",
                table: "DomainEvents");

            migrationBuilder.DropColumn(
                name: "Culture",
                schema: "domainEvents",
                table: "DomainEvents");

            migrationBuilder.DropColumn(
                name: "Source",
                schema: "domainEvents",
                table: "DomainEvents");

            migrationBuilder.DropColumn(
                name: "TenantId",
                schema: "domainEvents",
                table: "DomainEvents");
        }
    }
}
