using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NumbatWallet.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddPresentationRequests : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "PresentationRequests",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<string>(type: "text", nullable: false),
                    verifier_id = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    purpose = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    requested_credential_type = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    requested_claims_json = table.Column<string>(type: "jsonb", nullable: false),
                    presentation_definition_json = table.Column<string>(type: "jsonb", nullable: false),
                    nonce = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    status = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    expires_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    fulfilled_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    fulfilled_by_presentation_id = table.Column<Guid>(type: "uuid", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    modified_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    modified_by = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_presentation_requests", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "ix_presentation_requests_status",
                table: "PresentationRequests",
                column: "status");

            migrationBuilder.CreateIndex(
                name: "ix_presentation_requests_tenant_id",
                table: "PresentationRequests",
                column: "tenant_id");

            migrationBuilder.CreateIndex(
                name: "ix_presentation_requests_tenant_id_verifier_id",
                table: "PresentationRequests",
                columns: new[] { "tenant_id", "verifier_id" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PresentationRequests");
        }
    }
}
