using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace NumbatWallet.Infrastructure.Data.Migrations;

public partial class AddCertificateManagement : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        // Create TenantCertificates table
        migrationBuilder.CreateTable(
            name: "tenant_certificates",
            columns: table => new
            {
                id = table.Column<Guid>(type: "uuid", nullable: false),
                tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                certificate_data = table.Column<string>(type: "text", nullable: false),
                thumbprint = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: false),
                subject_dn = table.Column<string>(type: "varchar(500)", maxLength: 500, nullable: false),
                issuer_dn = table.Column<string>(type: "varchar(500)", maxLength: 500, nullable: false),
                serial_number = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: false),
                valid_from = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                valid_to = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                not_before = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                not_after = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                is_active = table.Column<bool>(type: "boolean", nullable: false),
                is_revoked = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                is_blocked = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                purpose = table.Column<int>(type: "integer", nullable: false),
                trust_level = table.Column<int>(type: "integer", nullable: false),
                created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                revoked_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                revocation_reason = table.Column<string>(type: "varchar(500)", maxLength: 500, nullable: true),
                last_used_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                usage_count = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                key_usage = table.Column<string>(type: "varchar(200)", maxLength: 200, nullable: true),
                extended_key_usage = table.Column<string>(type: "varchar(500)", maxLength: 500, nullable: true),
                created_by = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: true),
                updated_by = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("pk_tenant_certificates", x => x.id);
                table.ForeignKey(
                    name: "fk_tenant_certificates_tenants_tenant_id",
                    column: x => x.tenant_id,
                    principalTable: "tenants",
                    principalColumn: "id",
                    onDelete: ReferentialAction.Cascade);
            });

        // Create CertificateAuthorities table
        migrationBuilder.CreateTable(
            name: "certificate_authorities",
            columns: table => new
            {
                id = table.Column<Guid>(type: "uuid", nullable: false),
                name = table.Column<string>(type: "varchar(200)", maxLength: 200, nullable: false),
                certificate_data = table.Column<string>(type: "text", nullable: false),
                thumbprint = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: false),
                subject_dn = table.Column<string>(type: "varchar(500)", maxLength: 500, nullable: false),
                is_trusted = table.Column<bool>(type: "boolean", nullable: false),
                trust_level = table.Column<int>(type: "integer", nullable: false),
                crl_url = table.Column<string>(type: "varchar(500)", maxLength: 500, nullable: true),
                ocsp_url = table.Column<string>(type: "varchar(500)", maxLength: 500, nullable: true),
                valid_from = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                valid_to = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                last_validated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                created_by = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: true),
                updated_by = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("pk_certificate_authorities", x => x.id);
            });

        // Create CertificateTrustStores table
        migrationBuilder.CreateTable(
            name: "certificate_trust_stores",
            columns: table => new
            {
                id = table.Column<Guid>(type: "uuid", nullable: false),
                tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                name = table.Column<string>(type: "varchar(200)", maxLength: 200, nullable: false),
                description = table.Column<string>(type: "varchar(500)", maxLength: 500, nullable: false),
                is_active = table.Column<bool>(type: "boolean", nullable: false),
                trusted_certificate_ids = table.Column<string>(type: "text", nullable: true), // JSON array
                trusted_authority_ids = table.Column<string>(type: "text", nullable: true), // JSON array
                revoked_thumbprints = table.Column<string>(type: "text", nullable: true), // JSON array
                created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                created_by = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: true),
                updated_by = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("pk_certificate_trust_stores", x => x.id);
                table.ForeignKey(
                    name: "fk_certificate_trust_stores_tenants_tenant_id",
                    column: x => x.tenant_id,
                    principalTable: "tenants",
                    principalColumn: "id",
                    onDelete: ReferentialAction.Cascade);
            });

        // Create indexes
        migrationBuilder.CreateIndex(
            name: "ix_tenant_certificates_tenant_id",
            table: "tenant_certificates",
            column: "tenant_id");

        migrationBuilder.CreateIndex(
            name: "ix_tenant_certificates_thumbprint",
            table: "tenant_certificates",
            column: "thumbprint",
            unique: true);

        migrationBuilder.CreateIndex(
            name: "ix_tenant_certificates_purpose",
            table: "tenant_certificates",
            column: "purpose");

        migrationBuilder.CreateIndex(
            name: "ix_tenant_certificates_valid_to",
            table: "tenant_certificates",
            column: "valid_to");

        migrationBuilder.CreateIndex(
            name: "ix_certificate_authorities_thumbprint",
            table: "certificate_authorities",
            column: "thumbprint",
            unique: true);

        migrationBuilder.CreateIndex(
            name: "ix_certificate_authorities_subject_dn",
            table: "certificate_authorities",
            column: "subject_dn");

        migrationBuilder.CreateIndex(
            name: "ix_certificate_trust_stores_tenant_id",
            table: "certificate_trust_stores",
            column: "tenant_id");

        migrationBuilder.CreateIndex(
            name: "ix_certificate_trust_stores_name",
            table: "certificate_trust_stores",
            column: "name");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(
            name: "tenant_certificates");

        migrationBuilder.DropTable(
            name: "certificate_authorities");

        migrationBuilder.DropTable(
            name: "certificate_trust_stores");
    }
}
