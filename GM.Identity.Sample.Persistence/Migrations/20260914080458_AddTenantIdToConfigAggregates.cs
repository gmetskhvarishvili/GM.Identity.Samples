using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GM.Identity.Sample.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddTenantIdToConfigAggregates : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "TenantId",
                schema: "application",
                table: "Scope",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "TenantId",
                schema: "accessControl",
                table: "Roles",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "TenantId",
                schema: "accessControl",
                table: "Permissions",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "TenantId",
                schema: "accessControl",
                table: "Operations",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "TenantId",
                schema: "identity",
                table: "Clients",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Scope_TenantId",
                schema: "application",
                table: "Scope",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_Roles_TenantId",
                schema: "accessControl",
                table: "Roles",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_Permissions_TenantId",
                schema: "accessControl",
                table: "Permissions",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_Operations_TenantId",
                schema: "accessControl",
                table: "Operations",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_Clients_TenantId",
                schema: "identity",
                table: "Clients",
                column: "TenantId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Scope_TenantId",
                schema: "application",
                table: "Scope");

            migrationBuilder.DropIndex(
                name: "IX_Roles_TenantId",
                schema: "accessControl",
                table: "Roles");

            migrationBuilder.DropIndex(
                name: "IX_Permissions_TenantId",
                schema: "accessControl",
                table: "Permissions");

            migrationBuilder.DropIndex(
                name: "IX_Operations_TenantId",
                schema: "accessControl",
                table: "Operations");

            migrationBuilder.DropIndex(
                name: "IX_Clients_TenantId",
                schema: "identity",
                table: "Clients");

            migrationBuilder.DropColumn(
                name: "TenantId",
                schema: "application",
                table: "Scope");

            migrationBuilder.DropColumn(
                name: "TenantId",
                schema: "accessControl",
                table: "Roles");

            migrationBuilder.DropColumn(
                name: "TenantId",
                schema: "accessControl",
                table: "Permissions");

            migrationBuilder.DropColumn(
                name: "TenantId",
                schema: "accessControl",
                table: "Operations");

            migrationBuilder.DropColumn(
                name: "TenantId",
                schema: "identity",
                table: "Clients");
        }
    }
}
