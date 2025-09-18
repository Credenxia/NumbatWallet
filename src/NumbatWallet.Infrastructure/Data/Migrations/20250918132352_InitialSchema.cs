using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NumbatWallet.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class InitialSchema : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterDatabase()
                .Annotation("Npgsql:PostgresExtension:pgcrypto", ",,");

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
                name: "Persons",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    Email = table.Column<string>(type: "jsonb", nullable: false),
                    PhoneNumberValue = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
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
                name: "revocation_registry",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    issuer_id = table.Column<Guid>(type: "uuid", nullable: false),
                    registry_id = table.Column<string>(type: "text", nullable: false),
                    credential_type = table.Column<string>(type: "text", nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    max_credentials = table.Column<int>(type: "integer", nullable: false),
                    current_credentials = table.Column<int>(type: "integer", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    full_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_revocation_registry", x => x.id);
                    table.ForeignKey(
                        name: "fk_revocation_registry_issuers_issuer_id",
                        column: x => x.issuer_id,
                        principalTable: "Issuers",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "supported_credential_type",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    issuer_id = table.Column<Guid>(type: "uuid", nullable: false),
                    type_name = table.Column<string>(type: "text", nullable: false),
                    schema_id = table.Column<string>(type: "text", nullable: false),
                    schema_version = table.Column<string>(type: "text", nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_supported_credential_type", x => x.id);
                    table.ForeignKey(
                        name: "fk_supported_credential_type_issuers_issuer_id",
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
                    wallet_did = table.Column<string>(type: "text", nullable: false),
                    status = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    suspension_reason = table.Column<string>(type: "text", nullable: true),
                    lock_reason = table.Column<string>(type: "text", nullable: true),
                    external_id = table.Column<string>(type: "text", nullable: true),
                    expires_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
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
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_credentials_wallets_wallet_id",
                        column: x => x.wallet_id,
                        principalTable: "Wallets",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

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
                name: "ix_revocation_registry_issuer_id",
                table: "revocation_registry",
                column: "issuer_id");

            migrationBuilder.CreateIndex(
                name: "ix_supported_credential_type_issuer_id",
                table: "supported_credential_type",
                column: "issuer_id");

            migrationBuilder.CreateIndex(
                name: "ix_wallets_person_id",
                table: "Wallets",
                column: "person_id");

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
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Credentials");

            migrationBuilder.DropTable(
                name: "revocation_registry");

            migrationBuilder.DropTable(
                name: "supported_credential_type");

            migrationBuilder.DropTable(
                name: "Wallets");

            migrationBuilder.DropTable(
                name: "Issuers");

            migrationBuilder.DropTable(
                name: "Persons");
        }
    }
}
