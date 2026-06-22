using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NumbatWallet.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddPersonSearchTokenColumns : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_persons_email",
                table: "Persons");

            migrationBuilder.DropIndex(
                name: "ix_persons_phone_number_value",
                table: "Persons");

            migrationBuilder.AddColumn<string>(
                name: "email_search_token",
                table: "Persons",
                type: "character varying(64)",
                maxLength: 64,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "phone_search_token",
                table: "Persons",
                type: "character varying(64)",
                maxLength: 64,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "ix_persons_email_search_token",
                table: "Persons",
                column: "email_search_token");

            migrationBuilder.CreateIndex(
                name: "ix_persons_phone_search_token",
                table: "Persons",
                column: "phone_search_token");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_persons_email_search_token",
                table: "Persons");

            migrationBuilder.DropIndex(
                name: "ix_persons_phone_search_token",
                table: "Persons");

            migrationBuilder.DropColumn(
                name: "email_search_token",
                table: "Persons");

            migrationBuilder.DropColumn(
                name: "phone_search_token",
                table: "Persons");

            migrationBuilder.CreateIndex(
                name: "ix_persons_email",
                table: "Persons",
                column: "Email");

            migrationBuilder.CreateIndex(
                name: "ix_persons_phone_number_value",
                table: "Persons",
                column: "PhoneNumberValue");
        }
    }
}
