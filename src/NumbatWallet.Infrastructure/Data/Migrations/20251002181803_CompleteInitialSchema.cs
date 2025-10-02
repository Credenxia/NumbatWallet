using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NumbatWallet.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class CompleteInitialSchema : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Enable PostgreSQL extensions
            migrationBuilder.AlterDatabase()
                .Annotation("Npgsql:PostgresExtension:pgcrypto", ",,");

            // 1. TENANTS TABLE
            migrationBuilder.CreateTable(
                name: "tenants",
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

            migrationBuilder.CreateIndex(
                name: "ix_tenants_identifier",
                table: "tenants",
                column: "identifier",
                unique: true);

            // 2. PERSONS TABLE
            migrationBuilder.CreateTable(
                name: "persons",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    first_name = table.Column<string>(type: "jsonb", nullable: false),
                    last_name = table.Column<string>(type: "jsonb", nullable: false),
                    date_of_birth = table.Column<string>(type: "jsonb", nullable: false),
                    verified_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    verification_level = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    email = table.Column<string>(type: "jsonb", nullable: false),
                    phone_number_value = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    phone_number_country_code = table.Column<string>(type: "character varying(5)", maxLength: 5, nullable: true),
                    pin_hash = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    failed_pin_attempts = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    pin_locked_until = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    last_pin_attempt_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    tenant_id = table.Column<string>(type: "text", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    modified_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    modified_by = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_persons", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "ix_persons_email",
                table: "persons",
                column: "email");

            migrationBuilder.CreateIndex(
                name: "ix_persons_phone_number_value",
                table: "persons",
                column: "phone_number_value");

            migrationBuilder.CreateIndex(
                name: "ix_persons_tenant_id",
                table: "persons",
                column: "tenant_id");

            // 3. WALLETS TABLE
            migrationBuilder.CreateTable(
                name: "wallets",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    wallet_name = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    wallet_did = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                    person_id = table.Column<Guid>(type: "uuid", nullable: false),
                    status = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    suspension_reason = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    lock_reason = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    external_id = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    expires_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    tenant_id = table.Column<string>(type: "text", nullable: false),
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
                        principalTable: "persons",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "ix_wallets_person_id",
                table: "wallets",
                column: "person_id");

            migrationBuilder.CreateIndex(
                name: "ix_wallets_status",
                table: "wallets",
                column: "status");

            migrationBuilder.CreateIndex(
                name: "ix_wallets_tenant_id",
                table: "wallets",
                column: "tenant_id");

            migrationBuilder.CreateIndex(
                name: "ix_wallets_tenant_id_person_id",
                table: "wallets",
                columns: new[] { "tenant_id", "person_id" });

            migrationBuilder.CreateIndex(
                name: "ix_wallets_wallet_did",
                table: "wallets",
                column: "wallet_did",
                unique: true);

            // 4. ISSUERS TABLE
            migrationBuilder.CreateTable(
                name: "issuers",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    code = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    description = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    issuer_did = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                    public_key = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: false),
                    trusted_domain = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    deactivation_reason = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    tenant_id = table.Column<string>(type: "text", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    modified_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    modified_by = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_issuers", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "ix_issuers_code",
                table: "issuers",
                column: "code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_issuers_is_active",
                table: "issuers",
                column: "is_active");

            migrationBuilder.CreateIndex(
                name: "ix_issuers_tenant_id",
                table: "issuers",
                column: "tenant_id");

            migrationBuilder.CreateIndex(
                name: "ix_issuers_tenant_id_is_active",
                table: "issuers",
                columns: new[] { "tenant_id", "is_active" });

            migrationBuilder.CreateIndex(
                name: "ix_issuers_trusted_domain",
                table: "issuers",
                column: "trusted_domain",
                unique: true);

            // 5. CREDENTIALS TABLE
            migrationBuilder.CreateTable(
                name: "credentials",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    wallet_id = table.Column<Guid>(type: "uuid", nullable: false),
                    issuer_id = table.Column<Guid>(type: "uuid", nullable: false),
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
                        principalTable: "issuers",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_credentials_wallets_wallet_id",
                        column: x => x.wallet_id,
                        principalTable: "wallets",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_credentials_issuer_id",
                table: "credentials",
                column: "issuer_id");

            migrationBuilder.CreateIndex(
                name: "ix_credentials_status",
                table: "credentials",
                column: "status");

            migrationBuilder.CreateIndex(
                name: "ix_credentials_tenant_id",
                table: "credentials",
                column: "tenant_id");

            migrationBuilder.CreateIndex(
                name: "ix_credentials_tenant_id_status",
                table: "credentials",
                columns: new[] { "tenant_id", "status" });

            migrationBuilder.CreateIndex(
                name: "ix_credentials_tenant_id_wallet_id",
                table: "credentials",
                columns: new[] { "tenant_id", "wallet_id" });

            migrationBuilder.CreateIndex(
                name: "ix_credentials_wallet_id",
                table: "credentials",
                column: "wallet_id");

            migrationBuilder.CreateIndex(
                name: "ix_credentials_wallet_id_status",
                table: "credentials",
                columns: new[] { "wallet_id", "status" });

            // 6. WALLET TEMPLATES TABLE
            migrationBuilder.CreateTable(
                name: "wallet_templates",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    description = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    type = table.Column<string>(type: "text", nullable: false),
                    version = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    supported_credential_types = table.Column<string>(type: "jsonb", nullable: false),
                    metadata = table.Column<string>(type: "jsonb", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    created_by = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_wallet_templates", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "ix_wallet_templates_is_active",
                table: "wallet_templates",
                column: "is_active");

            migrationBuilder.CreateIndex(
                name: "ix_wallet_templates_tenant_id",
                table: "wallet_templates",
                column: "tenant_id");

            migrationBuilder.CreateIndex(
                name: "ix_wallet_templates_tenant_id_type",
                table: "wallet_templates",
                columns: new[] { "tenant_id", "type" });

            migrationBuilder.CreateIndex(
                name: "ix_wallet_templates_type",
                table: "wallet_templates",
                column: "type");

            // 7. WALLET TEMPLATE FIELDS TABLE (owned entity)
            migrationBuilder.CreateTable(
                name: "wallet_template_fields",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", Npgsql.EntityFrameworkCore.PostgreSQL.Metadata.NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    wallet_template_id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    label = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    field_type = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    is_required = table.Column<bool>(type: "boolean", nullable: false),
                    is_editable = table.Column<bool>(type: "boolean", nullable: false),
                    display_order = table.Column<int>(type: "integer", nullable: false),
                    validation_rule = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    default_value = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    mapped_credential_field = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    properties = table.Column<string>(type: "jsonb", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_wallet_template_fields", x => x.id);
                    table.ForeignKey(
                        name: "fk_wallet_template_fields_wallet_templates_wallet_template_id",
                        column: x => x.wallet_template_id,
                        principalTable: "wallet_templates",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_wallet_template_fields_wallet_template_id",
                table: "wallet_template_fields",
                column: "wallet_template_id");

            // 8. TENANT CERTIFICATES TABLE
            migrationBuilder.CreateTable(
                name: "tenant_certificates",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    certificate_data = table.Column<string>(type: "text", nullable: false),
                    thumbprint = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    subject_dn = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    issuer_dn = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    valid_from = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    valid_to = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    purpose = table.Column<string>(type: "text", nullable: false),
                    trust_level = table.Column<string>(type: "text", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    revoked_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    revocation_reason = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    last_used_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    usage_count = table.Column<int>(type: "integer", nullable: false),
                    serial_number = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    not_before = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    not_after = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    is_blocked = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_tenant_certificates", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "ix_tenant_certificates_tenant_id",
                table: "tenant_certificates",
                column: "tenant_id");

            migrationBuilder.CreateIndex(
                name: "ix_tenant_certificates_thumbprint",
                table: "tenant_certificates",
                column: "thumbprint");

            // 9. CERTIFICATE AUTHORITIES TABLE
            migrationBuilder.CreateTable(
                name: "certificate_authorities",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    certificate_data = table.Column<string>(type: "text", nullable: false),
                    thumbprint = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    subject_dn = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    is_trusted = table.Column<bool>(type: "boolean", nullable: false),
                    trust_level = table.Column<string>(type: "text", nullable: false),
                    crl_url = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: true),
                    ocsp_url = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: true),
                    valid_from = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    valid_to = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    last_validated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_certificate_authorities", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "ix_certificate_authorities_thumbprint",
                table: "certificate_authorities",
                column: "thumbprint",
                unique: true);

            // 10. CERTIFICATE TRUST STORES TABLE
            migrationBuilder.CreateTable(
                name: "certificate_trust_stores",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    description = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_certificate_trust_stores", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "ix_certificate_trust_stores_tenant_id",
                table: "certificate_trust_stores",
                column: "tenant_id");

            // 11. CERTIFICATE REVOCATIONS TABLE
            migrationBuilder.CreateTable(
                name: "certificate_revocations",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    serial_number = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    thumbprint = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    revocation_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    reason = table.Column<int>(type: "integer", nullable: false),
                    comment = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
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

            migrationBuilder.CreateIndex(
                name: "ix_certificate_revocations_serial_number",
                table: "certificate_revocations",
                column: "serial_number");

            // 12. ISSUANCES TABLE
            migrationBuilder.CreateTable(
                name: "issuances",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    credential_type = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    wallet_id = table.Column<Guid>(type: "uuid", nullable: false),
                    requester_id = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    status = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    requested_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    approved_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    approved_by = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    rejected_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    rejected_by = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    rejection_reason = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    completed_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    completed_by = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    credential_id = table.Column<Guid>(type: "uuid", nullable: true),
                    expiry_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_issuances", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "ix_issuances_tenant_id",
                table: "issuances",
                column: "tenant_id");

            migrationBuilder.CreateIndex(
                name: "ix_issuances_wallet_id",
                table: "issuances",
                column: "wallet_id");

            migrationBuilder.CreateIndex(
                name: "ix_issuances_status",
                table: "issuances",
                column: "status");

            // 13. REVOCATION REGISTRIES TABLE
            migrationBuilder.CreateTable(
                name: "revocation_registries",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    issuer_id = table.Column<Guid>(type: "uuid", nullable: false),
                    registry_id = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                    credential_type = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    max_credentials = table.Column<int>(type: "integer", nullable: false),
                    current_credentials = table.Column<int>(type: "integer", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    full_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_revocation_registries", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "ix_revocation_registries_issuer_id",
                table: "revocation_registries",
                column: "issuer_id");

            // 14. SUPPORTED CREDENTIAL TYPES TABLE
            migrationBuilder.CreateTable(
                name: "supported_credential_types",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    issuer_id = table.Column<Guid>(type: "uuid", nullable: false),
                    type_name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    schema_id = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                    schema_version = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_supported_credential_types", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "ix_supported_credential_types_issuer_id",
                table: "supported_credential_types",
                column: "issuer_id");

            // 15. AUDIT LOGS TABLE
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
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_audit_logs", x => x.id);
                });

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

            // 16. UNMASK AUDITS TABLE
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
                    unmasked_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    duration_seconds = table.Column<int>(type: "integer", nullable: false),
                    expires_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ip_address = table.Column<string>(type: "character varying(45)", maxLength: 45, nullable: true),
                    user_agent = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    approval_reference = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_unmask_audits", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "ix_unmask_audits_classification",
                table: "unmask_audits",
                column: "classification");

            migrationBuilder.CreateIndex(
                name: "ix_unmask_audits_entity",
                table: "unmask_audits",
                columns: new[] { "entity_type", "entity_id" });

            migrationBuilder.CreateIndex(
                name: "ix_unmask_audits_tenant_id",
                table: "unmask_audits",
                column: "tenant_id");

            migrationBuilder.CreateIndex(
                name: "ix_unmask_audits_tenant_id_unmasked_at",
                table: "unmask_audits",
                columns: new[] { "tenant_id", "unmasked_at" });

            migrationBuilder.CreateIndex(
                name: "ix_unmask_audits_unmasked_at",
                table: "unmask_audits",
                column: "unmasked_at");

            migrationBuilder.CreateIndex(
                name: "ix_unmask_audits_user_id",
                table: "unmask_audits",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "ix_unmask_audits_user_id_unmasked_at",
                table: "unmask_audits",
                columns: new[] { "user_id", "unmasked_at" });

            // 17. ADMIN USERS TABLE
            migrationBuilder.CreateTable(
                name: "admin_users",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    email = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    first_name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    last_name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    roles = table.Column<string>(type: "jsonb", nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    is_locked = table.Column<bool>(type: "boolean", nullable: false),
                    lock_reason = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    last_login_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    last_password_change_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_admin_users", x => x.id);
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

            // 18. EVENT STORE TABLE
            migrationBuilder.CreateTable(
                name: "event_store",
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
                    causation_id = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_event_store", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "ix_event_store_aggregate_id",
                table: "event_store",
                column: "aggregate_id");

            migrationBuilder.CreateIndex(
                name: "ix_event_store_aggregate_id_version",
                table: "event_store",
                columns: new[] { "aggregate_id", "version" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_event_store_aggregate_type",
                table: "event_store",
                column: "aggregate_type");

            migrationBuilder.CreateIndex(
                name: "ix_event_store_occurred_at",
                table: "event_store",
                column: "occurred_at");

            migrationBuilder.CreateIndex(
                name: "ix_event_store_tenant_id",
                table: "event_store",
                column: "tenant_id");

            // 19. EVENT SNAPSHOTS TABLE
            migrationBuilder.CreateTable(
                name: "event_snapshots",
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

            migrationBuilder.CreateIndex(
                name: "ix_event_snapshots_aggregate_id_version",
                table: "event_snapshots",
                columns: new[] { "aggregate_id", "version" });

            migrationBuilder.CreateIndex(
                name: "ix_event_snapshots_tenant_id",
                table: "event_snapshots",
                column: "tenant_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "event_snapshots");
            migrationBuilder.DropTable(name: "event_store");
            migrationBuilder.DropTable(name: "admin_users");
            migrationBuilder.DropTable(name: "unmask_audits");
            migrationBuilder.DropTable(name: "audit_logs");
            migrationBuilder.DropTable(name: "supported_credential_types");
            migrationBuilder.DropTable(name: "revocation_registries");
            migrationBuilder.DropTable(name: "issuances");
            migrationBuilder.DropTable(name: "certificate_revocations");
            migrationBuilder.DropTable(name: "certificate_trust_stores");
            migrationBuilder.DropTable(name: "certificate_authorities");
            migrationBuilder.DropTable(name: "tenant_certificates");
            migrationBuilder.DropTable(name: "wallet_template_fields");
            migrationBuilder.DropTable(name: "wallet_templates");
            migrationBuilder.DropTable(name: "credentials");
            migrationBuilder.DropTable(name: "issuers");
            migrationBuilder.DropTable(name: "wallets");
            migrationBuilder.DropTable(name: "persons");
            migrationBuilder.DropTable(name: "tenants");
        }
    }
}
