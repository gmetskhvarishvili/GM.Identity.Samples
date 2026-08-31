using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GM.Identity.Sample.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class Initial2 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ClientScopes_Scopes_ScopeId",
                schema: "accessControl",
                table: "ClientScopes");

            migrationBuilder.DropForeignKey(
                name: "FK_ScopeOperations_Scopes_ScopeId",
                schema: "accessControl",
                table: "ScopeOperations");

            migrationBuilder.DropForeignKey(
                name: "FK_UserSessions_Users_UserId",
                schema: "authorization",
                table: "UserSessions");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Scopes",
                schema: "accessControl",
                table: "Scopes");

            migrationBuilder.DropColumn(
                name: "TokenSalt",
                schema: "authorization",
                table: "UserSessions");

            migrationBuilder.DropColumn(
                name: "TokenSalt",
                schema: "authorization",
                table: "ClientSessions");

            migrationBuilder.EnsureSchema(
                name: "application");

            migrationBuilder.RenameTable(
                name: "Scopes",
                schema: "accessControl",
                newName: "Scope",
                newSchema: "application");

            migrationBuilder.AlterColumn<Guid>(
                name: "UserId",
                schema: "authorization",
                table: "UserSessions",
                type: "uuid",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uuid");

            migrationBuilder.AddColumn<Guid>(
                name: "ClientId",
                schema: "authorization",
                table: "UserSessions",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Provider",
                schema: "authorization",
                table: "UserSessions",
                type: "text",
                nullable: true);

            migrationBuilder.AlterColumn<DateTime>(
                name: "UpdatedAt",
                schema: "application",
                table: "Scope",
                type: "timestamp with time zone",
                nullable: true,
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone");

            migrationBuilder.AlterColumn<string>(
                name: "Name",
                schema: "application",
                table: "Scope",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(100)",
                oldMaxLength: 100);

            migrationBuilder.AlterColumn<bool>(
                name: "IsHidden",
                schema: "application",
                table: "Scope",
                type: "boolean",
                nullable: false,
                oldClrType: typeof(bool),
                oldType: "boolean",
                oldDefaultValue: false);

            migrationBuilder.AlterColumn<bool>(
                name: "IsDeleted",
                schema: "application",
                table: "Scope",
                type: "boolean",
                nullable: false,
                oldClrType: typeof(bool),
                oldType: "boolean",
                oldDefaultValue: false);

            migrationBuilder.AlterColumn<bool>(
                name: "IsActive",
                schema: "application",
                table: "Scope",
                type: "boolean",
                nullable: false,
                oldClrType: typeof(bool),
                oldType: "boolean",
                oldDefaultValue: true);

            migrationBuilder.AddPrimaryKey(
                name: "PK_Scope",
                schema: "application",
                table: "Scope",
                column: "Id");

            migrationBuilder.CreateIndex(
                name: "IX_UserSessions_ClientId",
                schema: "authorization",
                table: "UserSessions",
                column: "ClientId");

            migrationBuilder.AddForeignKey(
                name: "FK_ClientScopes_Scope_ScopeId",
                schema: "accessControl",
                table: "ClientScopes",
                column: "ScopeId",
                principalSchema: "application",
                principalTable: "Scope",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_ScopeOperations_Scope_ScopeId",
                schema: "accessControl",
                table: "ScopeOperations",
                column: "ScopeId",
                principalSchema: "application",
                principalTable: "Scope",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_UserSessions_Clients_ClientId",
                schema: "authorization",
                table: "UserSessions",
                column: "ClientId",
                principalSchema: "identity",
                principalTable: "Clients",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_UserSessions_Users_UserId",
                schema: "authorization",
                table: "UserSessions",
                column: "UserId",
                principalSchema: "identity",
                principalTable: "Users",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ClientScopes_Scope_ScopeId",
                schema: "accessControl",
                table: "ClientScopes");

            migrationBuilder.DropForeignKey(
                name: "FK_ScopeOperations_Scope_ScopeId",
                schema: "accessControl",
                table: "ScopeOperations");

            migrationBuilder.DropForeignKey(
                name: "FK_UserSessions_Clients_ClientId",
                schema: "authorization",
                table: "UserSessions");

            migrationBuilder.DropForeignKey(
                name: "FK_UserSessions_Users_UserId",
                schema: "authorization",
                table: "UserSessions");

            migrationBuilder.DropIndex(
                name: "IX_UserSessions_ClientId",
                schema: "authorization",
                table: "UserSessions");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Scope",
                schema: "application",
                table: "Scope");

            migrationBuilder.DropColumn(
                name: "ClientId",
                schema: "authorization",
                table: "UserSessions");

            migrationBuilder.DropColumn(
                name: "Provider",
                schema: "authorization",
                table: "UserSessions");

            migrationBuilder.RenameTable(
                name: "Scope",
                schema: "application",
                newName: "Scopes",
                newSchema: "accessControl");

            migrationBuilder.AlterColumn<Guid>(
                name: "UserId",
                schema: "authorization",
                table: "UserSessions",
                type: "uuid",
                nullable: false,
                defaultValue: Guid.Empty,
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);

            migrationBuilder.AddColumn<string>(
                name: "TokenSalt",
                schema: "authorization",
                table: "UserSessions",
                type: "character varying(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "TokenSalt",
                schema: "authorization",
                table: "ClientSessions",
                type: "character varying(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AlterColumn<DateTime>(
                name: "UpdatedAt",
                schema: "accessControl",
                table: "Scopes",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified),
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Name",
                schema: "accessControl",
                table: "Scopes",
                type: "character varying(100)",
                maxLength: 100,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AlterColumn<bool>(
                name: "IsHidden",
                schema: "accessControl",
                table: "Scopes",
                type: "boolean",
                nullable: false,
                defaultValue: false,
                oldClrType: typeof(bool),
                oldType: "boolean");

            migrationBuilder.AlterColumn<bool>(
                name: "IsDeleted",
                schema: "accessControl",
                table: "Scopes",
                type: "boolean",
                nullable: false,
                defaultValue: false,
                oldClrType: typeof(bool),
                oldType: "boolean");

            migrationBuilder.AlterColumn<bool>(
                name: "IsActive",
                schema: "accessControl",
                table: "Scopes",
                type: "boolean",
                nullable: false,
                defaultValue: true,
                oldClrType: typeof(bool),
                oldType: "boolean");

            migrationBuilder.AddPrimaryKey(
                name: "PK_Scopes",
                schema: "accessControl",
                table: "Scopes",
                column: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_ClientScopes_Scopes_ScopeId",
                schema: "accessControl",
                table: "ClientScopes",
                column: "ScopeId",
                principalSchema: "accessControl",
                principalTable: "Scopes",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_ScopeOperations_Scopes_ScopeId",
                schema: "accessControl",
                table: "ScopeOperations",
                column: "ScopeId",
                principalSchema: "accessControl",
                principalTable: "Scopes",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_UserSessions_Users_UserId",
                schema: "authorization",
                table: "UserSessions",
                column: "UserId",
                principalSchema: "identity",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
