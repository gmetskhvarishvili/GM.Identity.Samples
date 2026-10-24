using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GM.Identity.Sample.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class Initial : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "authorization");

            migrationBuilder.EnsureSchema(
                name: "identity");

            migrationBuilder.EnsureSchema(
                name: "access_control");

            migrationBuilder.EnsureSchema(
                name: "compliance");

            migrationBuilder.EnsureSchema(
                name: "domain_events");

            migrationBuilder.EnsureSchema(
                name: "outbox_messages");

            migrationBuilder.EnsureSchema(
                name: "application");

            migrationBuilder.CreateTable(
                name: "authorization_codes",
                schema: "authorization",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    created_by = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_by = table.Column<Guid>(type: "uuid", nullable: true),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: false),
                    is_hidden = table.Column<bool>(type: "boolean", nullable: false),
                    client_id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    code_hash = table.Column<string>(type: "text", nullable: false),
                    redirect_uri = table.Column<string>(type: "text", nullable: false),
                    scope = table.Column<string>(type: "text", nullable: true),
                    code_challenge = table.Column<string>(type: "text", nullable: false),
                    code_challenge_method = table.Column<string>(type: "text", nullable: false),
                    expires_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    consumed_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    sso_session_id = table.Column<Guid>(type: "uuid", nullable: true),
                    nonce = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_authorization_codes", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "client_redirect_uris",
                schema: "identity",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    created_by = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_by = table.Column<Guid>(type: "uuid", nullable: true),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: false),
                    is_hidden = table.Column<bool>(type: "boolean", nullable: false),
                    client_id = table.Column<Guid>(type: "uuid", nullable: false),
                    uri = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_client_redirect_uris", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "clients",
                schema: "identity",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    backchannel_logout_uri = table.Column<string>(type: "text", nullable: true),
                    frontchannel_logout_uri = table.Column<string>(type: "text", nullable: true),
                    require_consent = table.Column<bool>(type: "boolean", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_by = table.Column<Guid>(type: "uuid", nullable: true),
                    is_active = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    is_hidden = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: true),
                    name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    secret_hash = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    secret_salt = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_clients", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "consent_documents",
                schema: "compliance",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    created_by = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_by = table.Column<Guid>(type: "uuid", nullable: true),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: false),
                    is_hidden = table.Column<bool>(type: "boolean", nullable: false),
                    consent_type = table.Column<string>(type: "text", nullable: false),
                    title = table.Column<string>(type: "text", nullable: false),
                    content = table.Column<string>(type: "text", nullable: false),
                    version = table.Column<string>(type: "text", nullable: false),
                    is_mandatory = table.Column<bool>(type: "boolean", nullable: false),
                    is_current = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_consent_documents", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "device_codes",
                schema: "authorization",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    created_by = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_by = table.Column<Guid>(type: "uuid", nullable: true),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: false),
                    is_hidden = table.Column<bool>(type: "boolean", nullable: false),
                    client_id = table.Column<Guid>(type: "uuid", nullable: false),
                    device_code_hash = table.Column<string>(type: "text", nullable: false),
                    user_code = table.Column<string>(type: "text", nullable: false),
                    interval_seconds = table.Column<int>(type: "integer", nullable: false),
                    expires_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    status = table.Column<int>(type: "integer", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    last_polled_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_device_codes", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "domain_events",
                schema: "domain_events",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    aggregate_type = table.Column<string>(type: "text", nullable: true),
                    aggregate_id = table.Column<string>(type: "text", nullable: true),
                    event_type = table.Column<string>(type: "text", nullable: false),
                    payload = table.Column<string>(type: "text", nullable: false),
                    occurred_on = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    client_id = table.Column<Guid>(type: "uuid", nullable: true),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: true),
                    session_id = table.Column<Guid>(type: "uuid", nullable: true),
                    channel_id = table.Column<string>(type: "text", nullable: true),
                    culture = table.Column<string>(type: "text", nullable: true),
                    source = table.Column<string>(type: "text", nullable: true),
                    ip_address = table.Column<string>(type: "text", nullable: true),
                    correlation_id = table.Column<string>(type: "text", nullable: true),
                    user_agent = table.Column<string>(type: "text", nullable: true),
                    idempotency_key = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_domain_events", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "operations",
                schema: "access_control",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_by = table.Column<Guid>(type: "uuid", nullable: true),
                    is_active = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    is_hidden = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: true),
                    name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    description = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_operations", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "outbox_messages",
                schema: "outbox_messages",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    event_type = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    payload = table.Column<string>(type: "text", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    created_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    processed_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    error = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    retry_count = table.Column<int>(type: "integer", nullable: false, defaultValue: 0)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_outbox_messages", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "passkey_challenges",
                schema: "identity",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    created_by = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_by = table.Column<Guid>(type: "uuid", nullable: true),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: false),
                    is_hidden = table.Column<bool>(type: "boolean", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    challenge = table.Column<string>(type: "text", nullable: false),
                    expires_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_passkey_challenges", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "permissions",
                schema: "access_control",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_by = table.Column<Guid>(type: "uuid", nullable: true),
                    is_active = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    is_hidden = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: true),
                    name = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    description = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_permissions", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "roles",
                schema: "access_control",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_by = table.Column<Guid>(type: "uuid", nullable: true),
                    is_active = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    is_hidden = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: true),
                    name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_roles", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "scopes",
                schema: "access_control",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_by = table.Column<Guid>(type: "uuid", nullable: true),
                    is_active = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    is_hidden = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: true),
                    name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_scopes", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "sso_sessions",
                schema: "authorization",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    created_by = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_by = table.Column<Guid>(type: "uuid", nullable: true),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: false),
                    is_hidden = table.Column<bool>(type: "boolean", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    token_hash = table.Column<string>(type: "text", nullable: false),
                    auth_time = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    expires_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    revoked_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ip_address = table.Column<string>(type: "text", nullable: true),
                    user_agent = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_sso_sessions", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "two_factor_auth_types",
                schema: "identity",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_by = table.Column<Guid>(type: "uuid", nullable: true),
                    is_active = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    is_hidden = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    display_name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_two_factor_auth_types", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "user_client_consents",
                schema: "authorization",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    created_by = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_by = table.Column<Guid>(type: "uuid", nullable: true),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: false),
                    is_hidden = table.Column<bool>(type: "boolean", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    client_id = table.Column<Guid>(type: "uuid", nullable: false),
                    scopes = table.Column<string>(type: "text", nullable: false),
                    granted_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_user_client_consents", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "user_consents",
                schema: "compliance",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    created_by = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_by = table.Column<Guid>(type: "uuid", nullable: true),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: false),
                    is_hidden = table.Column<bool>(type: "boolean", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    consent_type = table.Column<string>(type: "text", nullable: false),
                    document_version = table.Column<string>(type: "text", nullable: false),
                    accepted_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_user_consents", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "user_passkeys",
                schema: "identity",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    created_by = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_by = table.Column<Guid>(type: "uuid", nullable: true),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: false),
                    is_hidden = table.Column<bool>(type: "boolean", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    credential_id = table.Column<string>(type: "text", nullable: false),
                    public_key_spki = table.Column<byte[]>(type: "bytea", nullable: false),
                    name = table.Column<string>(type: "text", nullable: false),
                    sign_count = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_user_passkeys", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "user_totp_devices",
                schema: "application",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    created_by = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_by = table.Column<Guid>(type: "uuid", nullable: true),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: false),
                    is_hidden = table.Column<bool>(type: "boolean", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    subject = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    secret_base32 = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                    is_confirmed = table.Column<bool>(type: "boolean", nullable: false),
                    created_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ConfirmedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_user_totp_devices", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "users",
                schema: "identity",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_by = table.Column<Guid>(type: "uuid", nullable: true),
                    is_active = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    is_hidden = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: true),
                    user_name = table.Column<string>(type: "text", nullable: false),
                    password_hash = table.Column<string>(type: "text", nullable: false),
                    password_salt = table.Column<string>(type: "text", nullable: false),
                    email = table.Column<string>(type: "text", nullable: true),
                    email_confirmed = table.Column<bool>(type: "boolean", nullable: false),
                    email_confirmed_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    phone_number = table.Column<string>(type: "text", nullable: true),
                    phone_number_confirmed = table.Column<bool>(type: "boolean", nullable: false),
                    phone_number_confirmed_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    lockout_end = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    lockout_enabled = table.Column<bool>(type: "boolean", nullable: false),
                    access_failed_count = table.Column<int>(type: "integer", nullable: false),
                    is_blocked = table.Column<bool>(type: "boolean", nullable: false),
                    blocked_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_users", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "client_sessions",
                schema: "authorization",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_by = table.Column<Guid>(type: "uuid", nullable: true),
                    is_active = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    is_hidden = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    client_id = table.Column<Guid>(type: "uuid", nullable: false),
                    token_hash = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    is_revoked = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    revoked_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    expires_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_client_sessions", x => x.id);
                    table.ForeignKey(
                        name: "fk_client_sessions_clients_client_id",
                        column: x => x.client_id,
                        principalSchema: "identity",
                        principalTable: "clients",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "role_permissions",
                schema: "access_control",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_by = table.Column<Guid>(type: "uuid", nullable: true),
                    is_active = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    is_hidden = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    role_id = table.Column<Guid>(type: "uuid", nullable: false),
                    permission_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_role_permissions", x => x.id);
                    table.ForeignKey(
                        name: "fk_role_permissions_permissions_permission_id",
                        column: x => x.permission_id,
                        principalSchema: "access_control",
                        principalTable: "permissions",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_role_permissions_roles_role_id",
                        column: x => x.role_id,
                        principalSchema: "access_control",
                        principalTable: "roles",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "client_scopes",
                schema: "access_control",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_by = table.Column<Guid>(type: "uuid", nullable: true),
                    is_active = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    is_hidden = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    scope_id = table.Column<Guid>(type: "uuid", nullable: false),
                    client_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_client_scopes", x => x.id);
                    table.ForeignKey(
                        name: "fk_client_scopes_clients_client_id",
                        column: x => x.client_id,
                        principalSchema: "identity",
                        principalTable: "clients",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_client_scopes_scope_scope_id",
                        column: x => x.scope_id,
                        principalSchema: "access_control",
                        principalTable: "scopes",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "scope_operations",
                schema: "access_control",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_by = table.Column<Guid>(type: "uuid", nullable: true),
                    is_active = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    is_hidden = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    scope_id = table.Column<Guid>(type: "uuid", nullable: false),
                    operation_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_scope_operations", x => x.id);
                    table.ForeignKey(
                        name: "fk_scope_operations_operations_operation_id",
                        column: x => x.operation_id,
                        principalSchema: "access_control",
                        principalTable: "operations",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_scope_operations_scopes_scope_id",
                        column: x => x.scope_id,
                        principalSchema: "access_control",
                        principalTable: "scopes",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "user_personal_info",
                schema: "identity",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    created_by = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_by = table.Column<Guid>(type: "uuid", nullable: true),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: false),
                    is_hidden = table.Column<bool>(type: "boolean", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    first_name = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    last_name = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    personal_number = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    birth_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_user_personal_info", x => x.id);
                    table.ForeignKey(
                        name: "fk_user_personal_info_user_user_id",
                        column: x => x.user_id,
                        principalSchema: "identity",
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "user_roles",
                schema: "access_control",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_by = table.Column<Guid>(type: "uuid", nullable: true),
                    is_active = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    is_hidden = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    role_id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_user_roles", x => x.id);
                    table.ForeignKey(
                        name: "fk_user_roles_roles_role_id",
                        column: x => x.role_id,
                        principalSchema: "access_control",
                        principalTable: "roles",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_user_roles_users_user_id",
                        column: x => x.user_id,
                        principalSchema: "identity",
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "user_sessions",
                schema: "authorization",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    ip_address = table.Column<string>(type: "text", nullable: true),
                    user_agent = table.Column<string>(type: "text", nullable: true),
                    channel_id = table.Column<string>(type: "text", nullable: true),
                    culture = table.Column<string>(type: "text", nullable: true),
                    correlation_id = table.Column<string>(type: "text", nullable: true),
                    idempotency_key = table.Column<string>(type: "text", nullable: true),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: true),
                    source = table.Column<string>(type: "text", nullable: true),
                    refresh_token_hash = table.Column<string>(type: "text", nullable: true),
                    sso_session_id = table.Column<Guid>(type: "uuid", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_by = table.Column<Guid>(type: "uuid", nullable: true),
                    is_active = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    is_hidden = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    client_id = table.Column<Guid>(type: "uuid", nullable: true),
                    provider = table.Column<string>(type: "text", nullable: true),
                    token_hash = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    two_factor_auth_type_id = table.Column<int>(type: "integer", nullable: false),
                    is_revoked = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    revoked_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    expires_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_user_sessions", x => x.id);
                    table.ForeignKey(
                        name: "fk_user_sessions_clients_client_id",
                        column: x => x.client_id,
                        principalSchema: "identity",
                        principalTable: "clients",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "fk_user_sessions_two_factor_auth_types_two_factor_auth_type_id",
                        column: x => x.two_factor_auth_type_id,
                        principalSchema: "identity",
                        principalTable: "two_factor_auth_types",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_user_sessions_users_user_id",
                        column: x => x.user_id,
                        principalSchema: "identity",
                        principalTable: "users",
                        principalColumn: "id");
                });

            migrationBuilder.CreateTable(
                name: "user_two_factor_auth_types",
                schema: "identity",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_by = table.Column<Guid>(type: "uuid", nullable: true),
                    is_active = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    is_hidden = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    two_factor_auth_type_id = table.Column<int>(type: "integer", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_user_two_factor_auth_types", x => x.id);
                    table.ForeignKey(
                        name: "fk_user_two_factor_auth_types_two_factor_auth_types_two_factor",
                        column: x => x.two_factor_auth_type_id,
                        principalSchema: "identity",
                        principalTable: "two_factor_auth_types",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_user_two_factor_auth_types_users_user_id",
                        column: x => x.user_id,
                        principalSchema: "identity",
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.InsertData(
                schema: "identity",
                table: "two_factor_auth_types",
                columns: new[] { "id", "created_at", "created_by", "display_name", "is_active", "name", "updated_at", "updated_by" },
                values: new object[] { 0, new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, "None", true, "None", new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null });

            migrationBuilder.CreateIndex(
                name: "ix_authorization_codes_code_hash",
                schema: "authorization",
                table: "authorization_codes",
                column: "code_hash");

            migrationBuilder.CreateIndex(
                name: "ix_client_redirect_uris_client_id",
                schema: "identity",
                table: "client_redirect_uris",
                column: "client_id");

            migrationBuilder.CreateIndex(
                name: "ix_client_scopes_client_id",
                schema: "access_control",
                table: "client_scopes",
                column: "client_id");

            migrationBuilder.CreateIndex(
                name: "ix_client_scopes_scope_id",
                schema: "access_control",
                table: "client_scopes",
                column: "scope_id");

            migrationBuilder.CreateIndex(
                name: "ix_client_sessions_client_id",
                schema: "authorization",
                table: "client_sessions",
                column: "client_id");

            migrationBuilder.CreateIndex(
                name: "ix_clients_tenant_id",
                schema: "identity",
                table: "clients",
                column: "tenant_id");

            migrationBuilder.CreateIndex(
                name: "ix_consent_documents_consent_type_is_current",
                schema: "compliance",
                table: "consent_documents",
                columns: new[] { "consent_type", "is_current" });

            migrationBuilder.CreateIndex(
                name: "ix_device_codes_device_code_hash",
                schema: "authorization",
                table: "device_codes",
                column: "device_code_hash");

            migrationBuilder.CreateIndex(
                name: "ix_device_codes_user_code",
                schema: "authorization",
                table: "device_codes",
                column: "user_code");

            migrationBuilder.CreateIndex(
                name: "ix_domain_events_aggregate_type_aggregate_id_occurred_on",
                schema: "domain_events",
                table: "domain_events",
                columns: new[] { "aggregate_type", "aggregate_id", "occurred_on" });

            migrationBuilder.CreateIndex(
                name: "ix_domain_events_correlation_id",
                schema: "domain_events",
                table: "domain_events",
                column: "correlation_id");

            migrationBuilder.CreateIndex(
                name: "ix_domain_events_tenant_id",
                schema: "domain_events",
                table: "domain_events",
                column: "tenant_id");

            migrationBuilder.CreateIndex(
                name: "ix_domain_events_user_id",
                schema: "domain_events",
                table: "domain_events",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "ix_operations_tenant_id",
                schema: "access_control",
                table: "operations",
                column: "tenant_id");

            migrationBuilder.CreateIndex(
                name: "ix_outbox_messages_processed_at",
                schema: "outbox_messages",
                table: "outbox_messages",
                column: "processed_at_utc");

            migrationBuilder.CreateIndex(
                name: "ix_outbox_messages_user_id",
                schema: "outbox_messages",
                table: "outbox_messages",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "ix_passkey_challenges_user_id",
                schema: "identity",
                table: "passkey_challenges",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "ix_permissions_tenant_id",
                schema: "access_control",
                table: "permissions",
                column: "tenant_id");

            migrationBuilder.CreateIndex(
                name: "ix_role_permissions_permission_id",
                schema: "access_control",
                table: "role_permissions",
                column: "permission_id");

            migrationBuilder.CreateIndex(
                name: "ix_role_permissions_role_id",
                schema: "access_control",
                table: "role_permissions",
                column: "role_id");

            migrationBuilder.CreateIndex(
                name: "ix_roles_tenant_id",
                schema: "access_control",
                table: "roles",
                column: "tenant_id");

            migrationBuilder.CreateIndex(
                name: "ix_scope_operations_operation_id",
                schema: "access_control",
                table: "scope_operations",
                column: "operation_id");

            migrationBuilder.CreateIndex(
                name: "ix_scope_operations_scope_id",
                schema: "access_control",
                table: "scope_operations",
                column: "scope_id");

            migrationBuilder.CreateIndex(
                name: "ix_scopes_tenant_id",
                schema: "access_control",
                table: "scopes",
                column: "tenant_id");

            migrationBuilder.CreateIndex(
                name: "ix_sso_sessions_token_hash",
                schema: "authorization",
                table: "sso_sessions",
                column: "token_hash");

            migrationBuilder.CreateIndex(
                name: "ix_sso_sessions_user_id",
                schema: "authorization",
                table: "sso_sessions",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "ix_user_client_consents_user_id_client_id",
                schema: "authorization",
                table: "user_client_consents",
                columns: new[] { "user_id", "client_id" });

            migrationBuilder.CreateIndex(
                name: "ix_user_consents_user_id_consent_type",
                schema: "compliance",
                table: "user_consents",
                columns: new[] { "user_id", "consent_type" });

            migrationBuilder.CreateIndex(
                name: "ix_user_passkeys_credential_id",
                schema: "identity",
                table: "user_passkeys",
                column: "credential_id");

            migrationBuilder.CreateIndex(
                name: "ix_user_passkeys_user_id",
                schema: "identity",
                table: "user_passkeys",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "ix_user_personal_info_user_id",
                schema: "identity",
                table: "user_personal_info",
                column: "user_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_user_roles_role_id",
                schema: "access_control",
                table: "user_roles",
                column: "role_id");

            migrationBuilder.CreateIndex(
                name: "ix_user_roles_user_id",
                schema: "access_control",
                table: "user_roles",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "ix_user_sessions_client_id",
                schema: "authorization",
                table: "user_sessions",
                column: "client_id");

            migrationBuilder.CreateIndex(
                name: "ix_user_sessions_two_factor_auth_type_id",
                schema: "authorization",
                table: "user_sessions",
                column: "two_factor_auth_type_id");

            migrationBuilder.CreateIndex(
                name: "ix_user_sessions_user_id",
                schema: "authorization",
                table: "user_sessions",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "ix_user_totp_devices_subject",
                schema: "application",
                table: "user_totp_devices",
                column: "subject");

            migrationBuilder.CreateIndex(
                name: "ix_user_totp_devices_user_id",
                schema: "application",
                table: "user_totp_devices",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "ix_user_two_factor_auth_types_two_factor_auth_type_id",
                schema: "identity",
                table: "user_two_factor_auth_types",
                column: "two_factor_auth_type_id");

            migrationBuilder.CreateIndex(
                name: "ix_user_two_factor_auth_types_user_id",
                schema: "identity",
                table: "user_two_factor_auth_types",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "ix_users_tenant_id",
                schema: "identity",
                table: "users",
                column: "tenant_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "authorization_codes",
                schema: "authorization");

            migrationBuilder.DropTable(
                name: "client_redirect_uris",
                schema: "identity");

            migrationBuilder.DropTable(
                name: "client_scopes",
                schema: "access_control");

            migrationBuilder.DropTable(
                name: "client_sessions",
                schema: "authorization");

            migrationBuilder.DropTable(
                name: "consent_documents",
                schema: "compliance");

            migrationBuilder.DropTable(
                name: "device_codes",
                schema: "authorization");

            migrationBuilder.DropTable(
                name: "domain_events",
                schema: "domain_events");

            migrationBuilder.DropTable(
                name: "outbox_messages",
                schema: "outbox_messages");

            migrationBuilder.DropTable(
                name: "passkey_challenges",
                schema: "identity");

            migrationBuilder.DropTable(
                name: "role_permissions",
                schema: "access_control");

            migrationBuilder.DropTable(
                name: "scope_operations",
                schema: "access_control");

            migrationBuilder.DropTable(
                name: "sso_sessions",
                schema: "authorization");

            migrationBuilder.DropTable(
                name: "user_client_consents",
                schema: "authorization");

            migrationBuilder.DropTable(
                name: "user_consents",
                schema: "compliance");

            migrationBuilder.DropTable(
                name: "user_passkeys",
                schema: "identity");

            migrationBuilder.DropTable(
                name: "user_personal_info",
                schema: "identity");

            migrationBuilder.DropTable(
                name: "user_roles",
                schema: "access_control");

            migrationBuilder.DropTable(
                name: "user_sessions",
                schema: "authorization");

            migrationBuilder.DropTable(
                name: "user_totp_devices",
                schema: "application");

            migrationBuilder.DropTable(
                name: "user_two_factor_auth_types",
                schema: "identity");

            migrationBuilder.DropTable(
                name: "permissions",
                schema: "access_control");

            migrationBuilder.DropTable(
                name: "operations",
                schema: "access_control");

            migrationBuilder.DropTable(
                name: "scopes",
                schema: "access_control");

            migrationBuilder.DropTable(
                name: "roles",
                schema: "access_control");

            migrationBuilder.DropTable(
                name: "clients",
                schema: "identity");

            migrationBuilder.DropTable(
                name: "two_factor_auth_types",
                schema: "identity");

            migrationBuilder.DropTable(
                name: "users",
                schema: "identity");
        }
    }
}
