using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GM.Identity.Sample.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddDomainEventMetadataAndActorAudit : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "CreatedBy",
                schema: "identity",
                table: "UserTwoFactorAuthTypes",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "UpdatedBy",
                schema: "identity",
                table: "UserTwoFactorAuthTypes",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "CreatedBy",
                schema: "authorization",
                table: "UserSessions",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "UpdatedBy",
                schema: "authorization",
                table: "UserSessions",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "CreatedBy",
                schema: "identity",
                table: "Users",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "UpdatedBy",
                schema: "identity",
                table: "Users",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "CreatedBy",
                schema: "accessControl",
                table: "UserRoles",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "UpdatedBy",
                schema: "accessControl",
                table: "UserRoles",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "CreatedBy",
                schema: "identity",
                table: "TwoFactorAuthTypes",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "UpdatedBy",
                schema: "identity",
                table: "TwoFactorAuthTypes",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "CreatedBy",
                schema: "accessControl",
                table: "ScopeOperations",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "UpdatedBy",
                schema: "accessControl",
                table: "ScopeOperations",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "CreatedBy",
                schema: "application",
                table: "Scope",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "UpdatedBy",
                schema: "application",
                table: "Scope",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "CreatedBy",
                schema: "accessControl",
                table: "Roles",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "UpdatedBy",
                schema: "accessControl",
                table: "Roles",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "CreatedBy",
                schema: "accessControl",
                table: "RolePermissions",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "UpdatedBy",
                schema: "accessControl",
                table: "RolePermissions",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "CreatedBy",
                schema: "accessControl",
                table: "Permissions",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "UpdatedBy",
                schema: "accessControl",
                table: "Permissions",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "CreatedBy",
                schema: "accessControl",
                table: "Operations",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "UpdatedBy",
                schema: "accessControl",
                table: "Operations",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "AggregateId",
                schema: "domainEvents",
                table: "DomainEvents",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "AggregateType",
                schema: "domainEvents",
                table: "DomainEvents",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CorrelationId",
                schema: "domainEvents",
                table: "DomainEvents",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "IdempotencyKey",
                schema: "domainEvents",
                table: "DomainEvents",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "IpAddress",
                schema: "domainEvents",
                table: "DomainEvents",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "SessionId",
                schema: "domainEvents",
                table: "DomainEvents",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "UserAgent",
                schema: "domainEvents",
                table: "DomainEvents",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "UserId",
                schema: "domainEvents",
                table: "DomainEvents",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "CreatedBy",
                schema: "authorization",
                table: "ClientSessions",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "UpdatedBy",
                schema: "authorization",
                table: "ClientSessions",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "CreatedBy",
                schema: "accessControl",
                table: "ClientScopes",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "UpdatedBy",
                schema: "accessControl",
                table: "ClientScopes",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "CreatedBy",
                schema: "identity",
                table: "Clients",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "UpdatedBy",
                schema: "identity",
                table: "Clients",
                type: "uuid",
                nullable: true);

            migrationBuilder.UpdateData(
                schema: "identity",
                table: "TwoFactorAuthTypes",
                keyColumn: "Id",
                keyValue: 0,
                columns: new[] { "CreatedBy", "UpdatedBy" },
                values: new object[] { null, null });

            migrationBuilder.CreateIndex(
                name: "IX_DomainEvents_AggregateType_AggregateId_OccurredOn",
                schema: "domainEvents",
                table: "DomainEvents",
                columns: new[] { "AggregateType", "AggregateId", "OccurredOn" });

            migrationBuilder.CreateIndex(
                name: "IX_DomainEvents_CorrelationId",
                schema: "domainEvents",
                table: "DomainEvents",
                column: "CorrelationId");

            migrationBuilder.CreateIndex(
                name: "IX_DomainEvents_UserId",
                schema: "domainEvents",
                table: "DomainEvents",
                column: "UserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_DomainEvents_AggregateType_AggregateId_OccurredOn",
                schema: "domainEvents",
                table: "DomainEvents");

            migrationBuilder.DropIndex(
                name: "IX_DomainEvents_CorrelationId",
                schema: "domainEvents",
                table: "DomainEvents");

            migrationBuilder.DropIndex(
                name: "IX_DomainEvents_UserId",
                schema: "domainEvents",
                table: "DomainEvents");

            migrationBuilder.DropColumn(
                name: "CreatedBy",
                schema: "identity",
                table: "UserTwoFactorAuthTypes");

            migrationBuilder.DropColumn(
                name: "UpdatedBy",
                schema: "identity",
                table: "UserTwoFactorAuthTypes");

            migrationBuilder.DropColumn(
                name: "CreatedBy",
                schema: "authorization",
                table: "UserSessions");

            migrationBuilder.DropColumn(
                name: "UpdatedBy",
                schema: "authorization",
                table: "UserSessions");

            migrationBuilder.DropColumn(
                name: "CreatedBy",
                schema: "identity",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "UpdatedBy",
                schema: "identity",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "CreatedBy",
                schema: "accessControl",
                table: "UserRoles");

            migrationBuilder.DropColumn(
                name: "UpdatedBy",
                schema: "accessControl",
                table: "UserRoles");

            migrationBuilder.DropColumn(
                name: "CreatedBy",
                schema: "identity",
                table: "TwoFactorAuthTypes");

            migrationBuilder.DropColumn(
                name: "UpdatedBy",
                schema: "identity",
                table: "TwoFactorAuthTypes");

            migrationBuilder.DropColumn(
                name: "CreatedBy",
                schema: "accessControl",
                table: "ScopeOperations");

            migrationBuilder.DropColumn(
                name: "UpdatedBy",
                schema: "accessControl",
                table: "ScopeOperations");

            migrationBuilder.DropColumn(
                name: "CreatedBy",
                schema: "application",
                table: "Scope");

            migrationBuilder.DropColumn(
                name: "UpdatedBy",
                schema: "application",
                table: "Scope");

            migrationBuilder.DropColumn(
                name: "CreatedBy",
                schema: "accessControl",
                table: "Roles");

            migrationBuilder.DropColumn(
                name: "UpdatedBy",
                schema: "accessControl",
                table: "Roles");

            migrationBuilder.DropColumn(
                name: "CreatedBy",
                schema: "accessControl",
                table: "RolePermissions");

            migrationBuilder.DropColumn(
                name: "UpdatedBy",
                schema: "accessControl",
                table: "RolePermissions");

            migrationBuilder.DropColumn(
                name: "CreatedBy",
                schema: "accessControl",
                table: "Permissions");

            migrationBuilder.DropColumn(
                name: "UpdatedBy",
                schema: "accessControl",
                table: "Permissions");

            migrationBuilder.DropColumn(
                name: "CreatedBy",
                schema: "accessControl",
                table: "Operations");

            migrationBuilder.DropColumn(
                name: "UpdatedBy",
                schema: "accessControl",
                table: "Operations");

            migrationBuilder.DropColumn(
                name: "AggregateId",
                schema: "domainEvents",
                table: "DomainEvents");

            migrationBuilder.DropColumn(
                name: "AggregateType",
                schema: "domainEvents",
                table: "DomainEvents");

            migrationBuilder.DropColumn(
                name: "CorrelationId",
                schema: "domainEvents",
                table: "DomainEvents");

            migrationBuilder.DropColumn(
                name: "IdempotencyKey",
                schema: "domainEvents",
                table: "DomainEvents");

            migrationBuilder.DropColumn(
                name: "IpAddress",
                schema: "domainEvents",
                table: "DomainEvents");

            migrationBuilder.DropColumn(
                name: "SessionId",
                schema: "domainEvents",
                table: "DomainEvents");

            migrationBuilder.DropColumn(
                name: "UserAgent",
                schema: "domainEvents",
                table: "DomainEvents");

            migrationBuilder.DropColumn(
                name: "UserId",
                schema: "domainEvents",
                table: "DomainEvents");

            migrationBuilder.DropColumn(
                name: "CreatedBy",
                schema: "authorization",
                table: "ClientSessions");

            migrationBuilder.DropColumn(
                name: "UpdatedBy",
                schema: "authorization",
                table: "ClientSessions");

            migrationBuilder.DropColumn(
                name: "CreatedBy",
                schema: "accessControl",
                table: "ClientScopes");

            migrationBuilder.DropColumn(
                name: "UpdatedBy",
                schema: "accessControl",
                table: "ClientScopes");

            migrationBuilder.DropColumn(
                name: "CreatedBy",
                schema: "identity",
                table: "Clients");

            migrationBuilder.DropColumn(
                name: "UpdatedBy",
                schema: "identity",
                table: "Clients");
        }
    }
}
