using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace NumbatWallet.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class InitialSchema : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "admin_users",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    email = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    first_name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    last_name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    is_locked = table.Column<bool>(type: "boolean", nullable: false),
                    lock_reason = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    last_login_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    last_password_change_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    roles = table.Column<string>(type: "jsonb", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_admin_users", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "audit_logs",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    action = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    entity_type = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    entity_id = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    old_values = table.Column<string>(type: "text", nullable: true),
                    new_values = table.Column<string>(type: "text", nullable: true),
                    ip_address = table.Column<string>(type: "character varying(45)", maxLength: 45, nullable: true),
                    user_agent = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    event_type = table.Column<string>(type: "text", nullable: false),
                    max_classification = table.Column<string>(type: "text", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_audit_logs", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "CertificateAuthorities",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    certificate_data = table.Column<string>(type: "text", nullable: false),
                    thumbprint = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    subject_dn = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    is_trusted = table.Column<bool>(type: "boolean", nullable: false),
                    trust_level = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    crl_url = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    ocsp_url = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    valid_from = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    valid_to = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    last_validated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_certificate_authorities", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "CertificateRevocations",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    serial_number = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    thumbprint = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    revocation_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    reason = table.Column<int>(type: "integer", nullable: false),
                    comment = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    revoked_by = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    invalidity_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    is_hold = table.Column<bool>(type: "boolean", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_certificate_revocations", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "CertificateTrustStores",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    description = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    RevokedThumbprints = table.Column<string>(type: "jsonb", nullable: false),
                    TrustedAuthorityIds = table.Column<string>(type: "jsonb", nullable: false),
                    TrustedCertificateIds = table.Column<string>(type: "jsonb", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_certificate_trust_stores", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "CredentialSchemas",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    schema_id = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                    name = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    description = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    version = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    type = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    Attributes = table.Column<string>(type: "jsonb", nullable: false),
                    Contexts = table.Column<string>(type: "jsonb", nullable: false),
                    Metadata = table.Column<string>(type: "jsonb", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_credential_schemas", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "EventSnapshots",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    aggregate_id = table.Column<Guid>(type: "uuid", nullable: false),
                    aggregate_type = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    snapshot_data = table.Column<string>(type: "text", nullable: false),
                    version = table.Column<int>(type: "integer", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    tenant_id = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_event_snapshots", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "EventStore",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    aggregate_id = table.Column<Guid>(type: "uuid", nullable: false),
                    aggregate_type = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    event_type = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    event_data = table.Column<string>(type: "text", nullable: false),
                    metadata = table.Column<string>(type: "text", nullable: true),
                    occurred_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    user_id = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    tenant_id = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    version = table.Column<int>(type: "integer", nullable: false),
                    correlation_id = table.Column<Guid>(type: "uuid", nullable: false),
                    causation_id = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_event_store", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "issuances",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    credential_type = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    wallet_id = table.Column<Guid>(type: "uuid", nullable: false),
                    requester_id = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    status = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    requested_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    approved_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    approved_by = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    rejected_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    rejected_by = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    rejection_reason = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    completed_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    completed_by = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    credential_id = table.Column<Guid>(type: "uuid", nullable: true),
                    expiry_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    claims = table.Column<string>(type: "jsonb", nullable: false),
                    metadata = table.Column<string>(type: "jsonb", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_issuances", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "Issuers",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<string>(type: "text", nullable: false),
                    external_id = table.Column<string>(type: "text", nullable: true),
                    name = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    code = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    description = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    issuer_did = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                    public_key = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: false),
                    endpoint = table.Column<string>(type: "text", nullable: false),
                    trusted_domain = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    deactivation_reason = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    status = table.Column<int>(type: "integer", nullable: false),
                    is_trusted = table.Column<bool>(type: "boolean", nullable: false),
                    trust_level = table.Column<int>(type: "integer", nullable: false),
                    jurisdiction = table.Column<string>(type: "text", nullable: true),
                    website_url = table.Column<string>(type: "text", nullable: true),
                    certificate_expires_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    modified_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    modified_by = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_issuers", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "Organizations",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    type = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    description = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    contact_email = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    contact_phone = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    website = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    tenant_id = table.Column<string>(type: "text", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    modified_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    modified_by = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_organizations", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "Persons",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    Email = table.Column<string>(type: "jsonb", nullable: false),
                    PhoneNumberValue = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    PhoneNumberCountryCode = table.Column<string>(type: "character varying(5)", maxLength: 5, nullable: true),
                    first_name = table.Column<string>(type: "jsonb", nullable: false),
                    last_name = table.Column<string>(type: "jsonb", nullable: false),
                    date_of_birth = table.Column<string>(type: "jsonb", nullable: false),
                    external_id = table.Column<string>(type: "text", nullable: false),
                    mobile_number = table.Column<string>(type: "text", nullable: true),
                    email_verified_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    email_verification_status = table.Column<int>(type: "integer", nullable: false),
                    phone_verification_status = table.Column<int>(type: "integer", nullable: false),
                    verified_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    verification_level = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    status = table.Column<int>(type: "integer", nullable: false),
                    tenant_id = table.Column<string>(type: "text", nullable: false),
                    pin_hash = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    failed_pin_attempts = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    pin_locked_until = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    last_pin_attempt_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    modified_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    modified_by = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_persons", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "TenantCertificates",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    certificate_data = table.Column<string>(type: "text", nullable: false),
                    thumbprint = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    subject_dn = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    issuer_dn = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    valid_from = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    valid_to = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    purpose = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    trust_level = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    revoked_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    revocation_reason = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    last_used_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    usage_count = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    serial_number = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    not_before = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    not_after = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    is_blocked = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_tenant_certificates", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "Tenants",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    identifier = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    subscription_tier = table.Column<string>(type: "text", nullable: false),
                    settings = table.Column<string>(type: "jsonb", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<string>(type: "text", nullable: true),
                    updated_by = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_tenants", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "unmask_audits",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    entity_type = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    entity_id = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    field_name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    classification = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    reason = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    user_id = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    unmasked_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    duration_seconds = table.Column<int>(type: "integer", nullable: false),
                    expires_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    ip_address = table.Column<string>(type: "character varying(45)", maxLength: 45, nullable: true),
                    user_agent = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    approval_reference = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_unmask_audits", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "WalletTemplates",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    description = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    type = table.Column<string>(type: "text", nullable: false),
                    version = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    created_by = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    metadata = table.Column<string>(type: "jsonb", nullable: false),
                    supported_credential_types = table.Column<string>(type: "jsonb", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_wallet_templates", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "RevocationRegistries",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    issuer_id = table.Column<Guid>(type: "uuid", nullable: false),
                    registry_id = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    credential_type = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    max_credentials = table.Column<int>(type: "integer", nullable: false),
                    current_credentials = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    full_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_revocation_registries", x => x.id);
                    table.ForeignKey(
                        name: "fk_revocation_registries_issuers_issuer_id",
                        column: x => x.issuer_id,
                        principalTable: "Issuers",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "SupportedCredentialTypes",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    issuer_id = table.Column<Guid>(type: "uuid", nullable: false),
                    type_name = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    schema_id = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                    schema_version = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_supported_credential_types", x => x.id);
                    table.ForeignKey(
                        name: "fk_supported_credential_types_issuers_issuer_id",
                        column: x => x.issuer_id,
                        principalTable: "Issuers",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Wallets",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    person_id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<string>(type: "text", nullable: false),
                    wallet_name = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    wallet_did = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                    type = table.Column<int>(type: "integer", nullable: false),
                    status = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    suspension_reason = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    lock_reason = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    external_id = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    expires_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    person_id1 = table.Column<Guid>(type: "uuid", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    modified_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    modified_by = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_wallets", x => x.id);
                    table.ForeignKey(
                        name: "fk_wallets_persons_person_id",
                        column: x => x.person_id,
                        principalTable: "Persons",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_wallets_persons_person_id1",
                        column: x => x.person_id1,
                        principalTable: "Persons",
                        principalColumn: "id");
                });

            migrationBuilder.CreateTable(
                name: "WalletTemplateFields",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    label = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    field_type = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    is_required = table.Column<bool>(type: "boolean", nullable: false),
                    is_editable = table.Column<bool>(type: "boolean", nullable: false),
                    display_order = table.Column<int>(type: "integer", nullable: false),
                    validation_rule = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    default_value = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    mapped_credential_field = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    properties = table.Column<string>(type: "jsonb", nullable: false),
                    wallet_template_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_wallet_template_fields", x => x.id);
                    table.ForeignKey(
                        name: "fk_wallet_template_fields_wallet_templates_wallet_template_id",
                        column: x => x.wallet_template_id,
                        principalTable: "WalletTemplates",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Credentials",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    wallet_id = table.Column<Guid>(type: "uuid", nullable: false),
                    issuer_id = table.Column<Guid>(type: "uuid", nullable: false),
                    credential_id = table.Column<string>(type: "text", nullable: false),
                    credential_type = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    credential_data = table.Column<string>(type: "jsonb", nullable: false),
                    schema_id = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                    status = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    issued_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    expires_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    revoked_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    revocation_reason = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    suspension_reason = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    tenant_id = table.Column<string>(type: "text", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    modified_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    modified_by = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_credentials", x => x.id);
                    table.ForeignKey(
                        name: "fk_credentials_issuers_issuer_id",
                        column: x => x.issuer_id,
                        principalTable: "Issuers",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_credentials_wallets_wallet_id",
                        column: x => x.wallet_id,
                        principalTable: "Wallets",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Presentations",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    credential_id = table.Column<Guid>(type: "uuid", nullable: false),
                    wallet_id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<string>(type: "text", nullable: false),
                    verifier_id = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    purpose = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    disclosed_claims_json = table.Column<string>(type: "jsonb", nullable: false),
                    status = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    presented_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    expires_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    verified_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    verification_count = table.Column<int>(type: "integer", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    modified_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    modified_by = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_presentations", x => x.id);
                    table.ForeignKey(
                        name: "fk_presentations_credentials_credential_id",
                        column: x => x.credential_id,
                        principalTable: "Credentials",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_admin_users_email",
                table: "admin_users",
                column: "email",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_admin_users_is_active",
                table: "admin_users",
                column: "is_active");

            migrationBuilder.CreateIndex(
                name: "ix_admin_users_last_name_first_name",
                table: "admin_users",
                columns: new[] { "last_name", "first_name" });

            migrationBuilder.CreateIndex(
                name: "ix_admin_users_tenant_id",
                table: "admin_users",
                column: "tenant_id");

            migrationBuilder.CreateIndex(
                name: "ix_audit_logs_created_at",
                table: "audit_logs",
                column: "created_at");

            migrationBuilder.CreateIndex(
                name: "ix_audit_logs_entity_id",
                table: "audit_logs",
                column: "entity_id");

            migrationBuilder.CreateIndex(
                name: "ix_audit_logs_entity_type",
                table: "audit_logs",
                column: "entity_type");

            migrationBuilder.CreateIndex(
                name: "ix_audit_logs_entity_type_entity_id",
                table: "audit_logs",
                columns: new[] { "entity_type", "entity_id" });

            migrationBuilder.CreateIndex(
                name: "ix_audit_logs_tenant_id",
                table: "audit_logs",
                column: "tenant_id");

            migrationBuilder.CreateIndex(
                name: "ix_audit_logs_tenant_id_created_at",
                table: "audit_logs",
                columns: new[] { "tenant_id", "created_at" });

            migrationBuilder.CreateIndex(
                name: "ix_audit_logs_user_id",
                table: "audit_logs",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "ix_certificate_authorities_is_trusted",
                table: "CertificateAuthorities",
                column: "is_trusted");

            migrationBuilder.CreateIndex(
                name: "ix_certificate_authorities_thumbprint",
                table: "CertificateAuthorities",
                column: "thumbprint",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_certificate_authorities_valid_to",
                table: "CertificateAuthorities",
                column: "valid_to");

            migrationBuilder.CreateIndex(
                name: "ix_certificate_revocations_is_hold",
                table: "CertificateRevocations",
                column: "is_hold");

            migrationBuilder.CreateIndex(
                name: "ix_certificate_revocations_revocation_date",
                table: "CertificateRevocations",
                column: "revocation_date");

            migrationBuilder.CreateIndex(
                name: "ix_certificate_revocations_serial_number",
                table: "CertificateRevocations",
                column: "serial_number");

            migrationBuilder.CreateIndex(
                name: "ix_certificate_revocations_thumbprint",
                table: "CertificateRevocations",
                column: "thumbprint");

            migrationBuilder.CreateIndex(
                name: "ix_certificate_trust_stores_tenant_id",
                table: "CertificateTrustStores",
                column: "tenant_id");

            migrationBuilder.CreateIndex(
                name: "ix_credentials_issuer_id",
                table: "Credentials",
                column: "issuer_id");

            migrationBuilder.CreateIndex(
                name: "ix_credentials_status",
                table: "Credentials",
                column: "status");

            migrationBuilder.CreateIndex(
                name: "ix_credentials_tenant_id",
                table: "Credentials",
                column: "tenant_id");

            migrationBuilder.CreateIndex(
                name: "ix_credentials_tenant_id_status",
                table: "Credentials",
                columns: new[] { "tenant_id", "status" });

            migrationBuilder.CreateIndex(
                name: "ix_credentials_tenant_id_wallet_id",
                table: "Credentials",
                columns: new[] { "tenant_id", "wallet_id" });

            migrationBuilder.CreateIndex(
                name: "ix_credentials_wallet_id",
                table: "Credentials",
                column: "wallet_id");

            migrationBuilder.CreateIndex(
                name: "ix_credentials_wallet_id_status",
                table: "Credentials",
                columns: new[] { "wallet_id", "status" });

            migrationBuilder.CreateIndex(
                name: "ix_credential_schemas_is_active",
                table: "CredentialSchemas",
                column: "is_active");

            migrationBuilder.CreateIndex(
                name: "ix_credential_schemas_name",
                table: "CredentialSchemas",
                column: "name");

            migrationBuilder.CreateIndex(
                name: "ix_credential_schemas_schema_id",
                table: "CredentialSchemas",
                column: "schema_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_credential_schemas_tenant_id",
                table: "CredentialSchemas",
                column: "tenant_id");

            migrationBuilder.CreateIndex(
                name: "ix_event_snapshots_aggregate_id_version",
                table: "EventSnapshots",
                columns: new[] { "aggregate_id", "version" });

            migrationBuilder.CreateIndex(
                name: "ix_event_snapshots_tenant_id",
                table: "EventSnapshots",
                column: "tenant_id");

            migrationBuilder.CreateIndex(
                name: "ix_event_store_aggregate_id",
                table: "EventStore",
                column: "aggregate_id");

            migrationBuilder.CreateIndex(
                name: "ix_event_store_aggregate_id_version",
                table: "EventStore",
                columns: new[] { "aggregate_id", "version" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_event_store_aggregate_type",
                table: "EventStore",
                column: "aggregate_type");

            migrationBuilder.CreateIndex(
                name: "ix_event_store_occurred_at",
                table: "EventStore",
                column: "occurred_at");

            migrationBuilder.CreateIndex(
                name: "ix_event_store_tenant_id",
                table: "EventStore",
                column: "tenant_id");

            migrationBuilder.CreateIndex(
                name: "ix_issuances_credential_type",
                table: "issuances",
                column: "credential_type");

            migrationBuilder.CreateIndex(
                name: "ix_issuances_status",
                table: "issuances",
                column: "status");

            migrationBuilder.CreateIndex(
                name: "ix_issuances_tenant_id",
                table: "issuances",
                column: "tenant_id");

            migrationBuilder.CreateIndex(
                name: "ix_issuances_tenant_id_status",
                table: "issuances",
                columns: new[] { "tenant_id", "status" });

            migrationBuilder.CreateIndex(
                name: "ix_issuances_tenant_id_wallet_id",
                table: "issuances",
                columns: new[] { "tenant_id", "wallet_id" });

            migrationBuilder.CreateIndex(
                name: "ix_issuances_wallet_id",
                table: "issuances",
                column: "wallet_id");

            migrationBuilder.CreateIndex(
                name: "ix_issuers_code",
                table: "Issuers",
                column: "code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_issuers_is_active",
                table: "Issuers",
                column: "is_active");

            migrationBuilder.CreateIndex(
                name: "ix_issuers_tenant_id",
                table: "Issuers",
                column: "tenant_id");

            migrationBuilder.CreateIndex(
                name: "ix_issuers_tenant_id_is_active",
                table: "Issuers",
                columns: new[] { "tenant_id", "is_active" });

            migrationBuilder.CreateIndex(
                name: "ix_issuers_trusted_domain",
                table: "Issuers",
                column: "trusted_domain",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_organizations_name",
                table: "Organizations",
                column: "name");

            migrationBuilder.CreateIndex(
                name: "ix_organizations_tenant_id",
                table: "Organizations",
                column: "tenant_id");

            migrationBuilder.CreateIndex(
                name: "ix_persons_email",
                table: "Persons",
                column: "Email");

            migrationBuilder.CreateIndex(
                name: "ix_persons_phone_number_value",
                table: "Persons",
                column: "PhoneNumberValue");

            migrationBuilder.CreateIndex(
                name: "ix_persons_tenant_id",
                table: "Persons",
                column: "tenant_id");

            migrationBuilder.CreateIndex(
                name: "ix_presentations_credential_id",
                table: "Presentations",
                column: "credential_id");

            migrationBuilder.CreateIndex(
                name: "ix_presentations_status",
                table: "Presentations",
                column: "status");

            migrationBuilder.CreateIndex(
                name: "ix_presentations_tenant_id",
                table: "Presentations",
                column: "tenant_id");

            migrationBuilder.CreateIndex(
                name: "ix_presentations_tenant_id_wallet_id",
                table: "Presentations",
                columns: new[] { "tenant_id", "wallet_id" });

            migrationBuilder.CreateIndex(
                name: "ix_presentations_wallet_id",
                table: "Presentations",
                column: "wallet_id");

            migrationBuilder.CreateIndex(
                name: "ix_revocation_registries_is_active",
                table: "RevocationRegistries",
                column: "is_active");

            migrationBuilder.CreateIndex(
                name: "ix_revocation_registries_issuer_id",
                table: "RevocationRegistries",
                column: "issuer_id");

            migrationBuilder.CreateIndex(
                name: "ix_revocation_registries_issuer_id_credential_type",
                table: "RevocationRegistries",
                columns: new[] { "issuer_id", "credential_type" });

            migrationBuilder.CreateIndex(
                name: "ix_revocation_registries_registry_id",
                table: "RevocationRegistries",
                column: "registry_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_supported_credential_types_is_active",
                table: "SupportedCredentialTypes",
                column: "is_active");

            migrationBuilder.CreateIndex(
                name: "ix_supported_credential_types_issuer_id",
                table: "SupportedCredentialTypes",
                column: "issuer_id");

            migrationBuilder.CreateIndex(
                name: "ix_supported_credential_types_issuer_id_type_name",
                table: "SupportedCredentialTypes",
                columns: new[] { "issuer_id", "type_name" });

            migrationBuilder.CreateIndex(
                name: "ix_supported_credential_types_type_name",
                table: "SupportedCredentialTypes",
                column: "type_name");

            migrationBuilder.CreateIndex(
                name: "ix_tenant_certificates_is_active",
                table: "TenantCertificates",
                column: "is_active");

            migrationBuilder.CreateIndex(
                name: "ix_tenant_certificates_serial_number",
                table: "TenantCertificates",
                column: "serial_number");

            migrationBuilder.CreateIndex(
                name: "ix_tenant_certificates_tenant_id",
                table: "TenantCertificates",
                column: "tenant_id");

            migrationBuilder.CreateIndex(
                name: "ix_tenant_certificates_thumbprint",
                table: "TenantCertificates",
                column: "thumbprint",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_tenant_certificates_valid_to",
                table: "TenantCertificates",
                column: "valid_to");

            migrationBuilder.CreateIndex(
                name: "ix_tenants_identifier",
                table: "Tenants",
                column: "identifier",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_UnmaskAudits_Classification",
                table: "unmask_audits",
                column: "classification");

            migrationBuilder.CreateIndex(
                name: "IX_UnmaskAudits_Entity",
                table: "unmask_audits",
                columns: new[] { "entity_type", "entity_id" });

            migrationBuilder.CreateIndex(
                name: "IX_UnmaskAudits_TenantId",
                table: "unmask_audits",
                column: "tenant_id");

            migrationBuilder.CreateIndex(
                name: "IX_UnmaskAudits_TenantId_UnmaskedAt",
                table: "unmask_audits",
                columns: new[] { "tenant_id", "unmasked_at" });

            migrationBuilder.CreateIndex(
                name: "IX_UnmaskAudits_UnmaskedAt",
                table: "unmask_audits",
                column: "unmasked_at");

            migrationBuilder.CreateIndex(
                name: "IX_UnmaskAudits_UserId",
                table: "unmask_audits",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "IX_UnmaskAudits_UserId_UnmaskedAt",
                table: "unmask_audits",
                columns: new[] { "user_id", "unmasked_at" });

            migrationBuilder.CreateIndex(
                name: "ix_wallets_person_id",
                table: "Wallets",
                column: "person_id");

            migrationBuilder.CreateIndex(
                name: "ix_wallets_person_id1",
                table: "Wallets",
                column: "person_id1");

            migrationBuilder.CreateIndex(
                name: "ix_wallets_status",
                table: "Wallets",
                column: "status");

            migrationBuilder.CreateIndex(
                name: "ix_wallets_tenant_id",
                table: "Wallets",
                column: "tenant_id");

            migrationBuilder.CreateIndex(
                name: "ix_wallets_tenant_id_person_id",
                table: "Wallets",
                columns: new[] { "tenant_id", "person_id" });

            migrationBuilder.CreateIndex(
                name: "ix_wallets_wallet_did",
                table: "Wallets",
                column: "wallet_did",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_wallet_template_fields_wallet_template_id",
                table: "WalletTemplateFields",
                column: "wallet_template_id");

            migrationBuilder.CreateIndex(
                name: "ix_wallet_templates_is_active",
                table: "WalletTemplates",
                column: "is_active");

            migrationBuilder.CreateIndex(
                name: "ix_wallet_templates_tenant_id",
                table: "WalletTemplates",
                column: "tenant_id");

            migrationBuilder.CreateIndex(
                name: "ix_wallet_templates_tenant_id_type",
                table: "WalletTemplates",
                columns: new[] { "tenant_id", "type" });

            migrationBuilder.CreateIndex(
                name: "ix_wallet_templates_type",
                table: "WalletTemplates",
                column: "type");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "admin_users");

            migrationBuilder.DropTable(
                name: "audit_logs");

            migrationBuilder.DropTable(
                name: "CertificateAuthorities");

            migrationBuilder.DropTable(
                name: "CertificateRevocations");

            migrationBuilder.DropTable(
                name: "CertificateTrustStores");

            migrationBuilder.DropTable(
                name: "CredentialSchemas");

            migrationBuilder.DropTable(
                name: "EventSnapshots");

            migrationBuilder.DropTable(
                name: "EventStore");

            migrationBuilder.DropTable(
                name: "issuances");

            migrationBuilder.DropTable(
                name: "Organizations");

            migrationBuilder.DropTable(
                name: "Presentations");

            migrationBuilder.DropTable(
                name: "RevocationRegistries");

            migrationBuilder.DropTable(
                name: "SupportedCredentialTypes");

            migrationBuilder.DropTable(
                name: "TenantCertificates");

            migrationBuilder.DropTable(
                name: "Tenants");

            migrationBuilder.DropTable(
                name: "unmask_audits");

            migrationBuilder.DropTable(
                name: "WalletTemplateFields");

            migrationBuilder.DropTable(
                name: "Credentials");

            migrationBuilder.DropTable(
                name: "WalletTemplates");

            migrationBuilder.DropTable(
                name: "Issuers");

            migrationBuilder.DropTable(
                name: "Wallets");

            migrationBuilder.DropTable(
                name: "Persons");
        }
    }
}
