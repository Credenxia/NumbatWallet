using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NumbatWallet.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class SeedTestData : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Test data for dashboard - generates realistic statistics
            var tenantId = "11111111-1111-1111-1111-111111111111";
            var personId = "22222222-2222-2222-2222-222222222222";
            var walletId = "33333333-3333-3333-3333-333333333333";
            var issuerId = "44444444-4444-4444-4444-444444444444";
            var now = DateTime.UtcNow;

            // Insert test tenant
            migrationBuilder.Sql($@"
                INSERT INTO ""Tenants"" (""id"", ""name"", ""identifier"", ""is_active"", ""subscription_tier"", ""settings"", ""created_at"", ""updated_at"")
                VALUES ('{tenantId}', 'Test Tenant', 'test-tenant', true, 'Trial', '{{}}', '{now:yyyy-MM-dd HH:mm:ss}', '{now:yyyy-MM-dd HH:mm:ss}');
            ");

            // Insert test person - using minimal required JSONB fields
            migrationBuilder.Sql($@"
                INSERT INTO ""Persons"" (""id"", ""external_id"", ""Email"", ""PhoneNumberValue"",
                    ""first_name"", ""last_name"", ""date_of_birth"",
                    ""email_verification_status"", ""phone_verification_status"", ""status"",
                    ""tenant_id"", ""failed_pin_attempts"", ""created_at"")
                VALUES ('{personId}', 'test-person-001',
                    '{{\""value\"":\""john.doe@test.com\""}}',
                    '+61400000000',
                    '{{\""value\"":\""John\""}}',
                    '{{\""value\"":\""Doe\""}}',
                    '{{\""value\"":\""1990-01-01\""}}',
                    0, 0, 1,
                    '{tenantId}', 0, '{now:yyyy-MM-dd HH:mm:ss}');
            ");

            // Insert test issuer
            migrationBuilder.Sql($@"
                INSERT INTO ""Issuers"" (""id"", ""name"", ""did"", ""is_trusted"", ""created_at"", ""updated_at"", ""tenant_id"")
                VALUES ('{issuerId}', 'Test Issuer', 'did:example:test', true, '{now:yyyy-MM-dd HH:mm:ss}', '{now:yyyy-MM-dd HH:mm:ss}', '{tenantId}');
            ");

            // Insert test wallet
            migrationBuilder.Sql($@"
                INSERT INTO ""Wallets"" (""id"", ""person_id"", ""tenant_id"", ""wallet_name"", ""wallet_did"",
                    ""type"", ""status"", ""created_at"", ""created_by"")
                VALUES ('{walletId}', '{personId}', '{tenantId}', 'Test Wallet', 'did:example:wallet:001',
                    0, 'Active', '{now:yyyy-MM-dd HH:mm:ss}', 'system');
            ");

            // Insert test credentials - active
            for (int i = 1; i <= 5; i++)
            {
                var credId = $"55555555-5555-5555-5555-55555555555{i}";
                migrationBuilder.Sql($@"
                    INSERT INTO ""Credentials"" (""id"", ""wallet_id"", ""issuer_id"", ""credential_id"", ""credential_type"",
                        ""credential_data"", ""schema_id"", ""status"", ""issued_at"", ""expires_at"", ""created_at"",
                        ""updated_at"", ""tenant_id"")
                    VALUES ('{credId}', '{walletId}', '{issuerId}', 'cred_{i:0000}', 'VerifiableCredential',
                        '{{\""claims\"":{{\""name\"":\""Test User {i}\""}}}}', 'schema-001', 1,
                        '{now:yyyy-MM-dd HH:mm:ss}', '{now.AddDays(365):yyyy-MM-dd HH:mm:ss}',
                        '{now:yyyy-MM-dd HH:mm:ss}', '{now:yyyy-MM-dd HH:mm:ss}', '{tenantId}');
                ");
            }

            // Insert credentials issued today
            for (int i = 1; i <= 3; i++)
            {
                var credId = $"66666666-6666-6666-6666-66666666666{i}";
                migrationBuilder.Sql($@"
                    INSERT INTO ""Credentials"" (""id"", ""wallet_id"", ""issuer_id"", ""credential_id"", ""credential_type"",
                        ""credential_data"", ""schema_id"", ""status"", ""issued_at"", ""expires_at"", ""created_at"",
                        ""updated_at"", ""tenant_id"")
                    VALUES ('{credId}', '{walletId}', '{issuerId}', 'cred_today_{i:0000}', 'VerifiableCredential',
                        '{{\""claims\"":{{\""name\"":\""New User {i}\""}}}}', 'schema-001', 1,
                        '{now.Date:yyyy-MM-dd HH:mm:ss}', '{now.AddDays(365):yyyy-MM-dd HH:mm:ss}',
                        '{now.Date:yyyy-MM-dd HH:mm:ss}', '{now.Date:yyyy-MM-dd HH:mm:ss}', '{tenantId}');
                ");
            }

            // Insert credentials expiring this week
            for (int i = 1; i <= 2; i++)
            {
                var credId = $"77777777-7777-7777-7777-77777777777{i}";
                var expiryDate = now.AddDays(i * 2); // Expires in 2, 4 days
                migrationBuilder.Sql($@"
                    INSERT INTO ""Credentials"" (""id"", ""wallet_id"", ""issuer_id"", ""credential_id"", ""credential_type"",
                        ""credential_data"", ""schema_id"", ""status"", ""issued_at"", ""expires_at"", ""created_at"",
                        ""updated_at"", ""tenant_id"")
                    VALUES ('{credId}', '{walletId}', '{issuerId}', 'cred_expiring_{i:0000}', 'VerifiableCredential',
                        '{{\""claims\"":{{\""name\"":\""Expiring User {i}\""}}}}', 'schema-001', 1,
                        '{now.AddDays(-30):yyyy-MM-dd HH:mm:ss}', '{expiryDate:yyyy-MM-dd HH:mm:ss}',
                        '{now.AddDays(-30):yyyy-MM-dd HH:mm:ss}', '{now:yyyy-MM-dd HH:mm:ss}', '{tenantId}');
                ");
            }
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Remove all test data
            migrationBuilder.Sql(@"DELETE FROM ""Credentials"" WHERE ""tenant_id"" = '11111111-1111-1111-1111-111111111111';");
            migrationBuilder.Sql(@"DELETE FROM ""Wallets"" WHERE ""tenant_id"" = '11111111-1111-1111-1111-111111111111';");
            migrationBuilder.Sql(@"DELETE FROM ""Issuers"" WHERE ""tenant_id"" = '11111111-1111-1111-1111-111111111111';");
            migrationBuilder.Sql(@"DELETE FROM ""Persons"" WHERE ""tenant_id"" = '11111111-1111-1111-1111-111111111111';");
            migrationBuilder.Sql(@"DELETE FROM ""Tenants"" WHERE ""id"" = '11111111-1111-1111-1111-111111111111';");
        }
    }
}
