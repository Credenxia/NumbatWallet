using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NumbatWallet.Domain.Aggregates;
using NumbatWallet.Domain.Entities;

namespace NumbatWallet.Infrastructure.Data;

public class DatabaseSeeder : IDatabaseSeeder
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<DatabaseSeeder> _logger;

    public DatabaseSeeder(
        IServiceProvider serviceProvider,
        ILogger<DatabaseSeeder> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    public async Task SeedAsync(CancellationToken cancellationToken = default)
    {
        using var scope = _serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<NumbatWalletDbContext>();

        // Get tenant service from the scope
        var tenantService = scope.ServiceProvider.GetService<SharedKernel.Interfaces.ITenantService>();
        var currentTenantId = tenantService?.TenantId.ToString() ?? "00000000-0000-0000-0000-000000000000";
        _logger.LogInformation("Using tenant ID for seeding: {TenantId}", currentTenantId);

        try
        {
            // Don't use both EnsureCreated and Migrate - they conflict
            // Try migrations first (production), fall back to EnsureCreated (tests)
            if (context.Database.IsRelational())
            {
                try
                {
                    var pendingMigrations = await context.Database.GetPendingMigrationsAsync(cancellationToken);
                    if (pendingMigrations.Any())
                    {
                        await context.Database.MigrateAsync(cancellationToken);
                        _logger.LogInformation("Database migrations applied successfully");
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Migrations failed, using EnsureCreated for tests");
                    await context.Database.EnsureCreatedAsync(cancellationToken);
                }
            }
            else
            {
                await context.Database.EnsureCreatedAsync(cancellationToken);
            }

            // Seed data
            await SeedTenantsAsync(context, cancellationToken);
            await SeedIssuersAsync(context, currentTenantId, cancellationToken);
            await SeedTestPersonsAsync(context, currentTenantId, cancellationToken);

            await context.SaveChangesAsync(cancellationToken);
            _logger.LogInformation("Database seeding completed successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred while seeding the database");
            throw;
        }
    }

    private async Task SeedTenantsAsync(NumbatWalletDbContext context, CancellationToken cancellationToken)
    {
        if (await context.Tenants.AnyAsync(cancellationToken))
        {
            _logger.LogInformation("Tenants already seeded");
            return;
        }

        var defaultTenant = new Tenant(Guid.NewGuid())
        {
            Name = "Default Development Tenant",
            Identifier = Guid.Empty.ToString(), // "00000000-0000-0000-0000-000000000000"
            IsActive = true,
            SubscriptionTier = "Enterprise",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            CreatedBy = "system",
            UpdatedBy = "system",
            Settings = new Dictionary<string, object>
            {
                ["features"] = new List<string> { "all" },
                ["maxUsers"] = 1000,
                ["maxWallets"] = 10000,
                ["allowedPlatforms"] = new List<string> { "apple", "google", "web" }
            }
        };

        var devTenant = new Tenant(Guid.NewGuid())
        {
            Name = "Development Test Tenant",
            Identifier = "dev-tenant",
            IsActive = true,
            SubscriptionTier = "Professional",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            CreatedBy = "system",
            UpdatedBy = "system",
            Settings = new Dictionary<string, object>
            {
                ["features"] = new List<string> { "wallet-generation", "credential-issuance", "verification" },
                ["maxUsers"] = 100,
                ["maxWallets"] = 1000,
                ["allowedPlatforms"] = new List<string> { "apple", "google", "web" }
            }
        };

        var tenants = new[] { defaultTenant, devTenant };
        await context.Tenants.AddRangeAsync(tenants, cancellationToken);
        _logger.LogInformation("Seeded {Count} tenants", tenants.Length);
    }

    private async Task SeedIssuersAsync(NumbatWalletDbContext context, string currentTenantId, CancellationToken cancellationToken)
    {
        // Check if specific issuer already exists to prevent duplicates
        if (await context.Issuers.AnyAsync(i => i.Code == "WA-DOT", cancellationToken))
        {
            _logger.LogInformation("Issuers already seeded");
            return;
        }

        // Create issuers
        var transportIssuer = new Issuer(
            name: "Western Australia Department of Transport",
            code: "WA-DOT",
            issuerDid: "did:web:transport.wa.gov.au",
            publicKey: "MIIBIjANBgkqhkiG9w0BAQEFAAOCAQ8AMIIBCgKCAQEA...",
            endpoint: "https://api.transport.wa.gov.au/credentials",
            tenantId: currentTenantId)
        {
            CreatedAt = DateTimeOffset.UtcNow,
            CreatedBy = "system"
        };
        transportIssuer.UpdateDetails("Western Australia Department of Transport",
            "Official issuer for WA driving licenses and vehicle registrations");
        transportIssuer.MarkAsTrusted(10);
        transportIssuer.SetJurisdiction("Western Australia");
        transportIssuer.SetWebsiteUrl("https://www.transport.wa.gov.au");
        transportIssuer.SetTrustedDomain("transport.wa.gov.au");

        var healthIssuer = new Issuer(
            name: "Western Australia Department of Health",
            code: "WA-HEALTH",
            issuerDid: "did:web:health.wa.gov.au",
            publicKey: "MIIBIjANBgkqhkiG9w0BAQEFAAOCAQ8AMIIBCgKCAQEB...",
            endpoint: "https://api.health.wa.gov.au/credentials",
            tenantId: currentTenantId)
        {
            CreatedAt = DateTimeOffset.UtcNow,
            CreatedBy = "system"
        };
        healthIssuer.UpdateDetails("Western Australia Department of Health",
            "Official issuer for health and vaccination records");
        healthIssuer.MarkAsTrusted(10);
        healthIssuer.SetJurisdiction("Western Australia");
        healthIssuer.SetWebsiteUrl("https://www.health.wa.gov.au");
        healthIssuer.SetTrustedDomain("health.wa.gov.au");

        var educationIssuer = new Issuer(
            name: "Western Australia Department of Education",
            code: "WA-EDU",
            issuerDid: "did:web:education.wa.gov.au",
            publicKey: "MIIBIjANBgkqhkiG9w0BAQEFAAOCAQ8AMIIBCgKCAQEC...",
            endpoint: "https://api.education.wa.gov.au/credentials",
            tenantId: currentTenantId)
        {
            CreatedAt = DateTimeOffset.UtcNow,
            CreatedBy = "system"
        };
        educationIssuer.UpdateDetails("Western Australia Department of Education",
            "Official issuer for educational credentials and certifications");
        educationIssuer.MarkAsTrusted(9);
        educationIssuer.SetJurisdiction("Western Australia");
        educationIssuer.SetWebsiteUrl("https://www.education.wa.gov.au");
        educationIssuer.SetTrustedDomain("education.wa.gov.au");

        var issuers = new[] { transportIssuer, healthIssuer, educationIssuer };

        await context.Issuers.AddRangeAsync(issuers, cancellationToken);
        _logger.LogInformation("Seeded {Count} issuers", issuers.Length);
    }

    private async Task SeedTestPersonsAsync(NumbatWalletDbContext context, string currentTenantId, CancellationToken cancellationToken)
    {
        // Only seed in development and testing environments
        var environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT");
        if (environment != "Development" && environment != "Testing")
        {
            return;
        }

        if (await context.Persons.AnyAsync(cancellationToken))
        {
            _logger.LogInformation("Test persons already seeded");
            return;
        }

        // Create test persons for integration tests with role-based emails
        // For testing, we'll use reflection to set verification status without going through verification code flow
        var citizen = new Person(
            firstName: "Test",
            lastName: "Citizen",
            dateOfBirth: new DateOnly(1990, 1, 1),
            email: "citizen@example.com",
            externalId: "TEST-CITIZEN-001",
            tenantId: currentTenantId);
        SetVerificationStatus(citizen, SharedKernel.Enums.VerificationStatus.Verified);

        var officer = new Person(
            firstName: "Test",
            lastName: "Officer",
            dateOfBirth: new DateOnly(1985, 6, 15),
            email: "officer@example.com",
            externalId: "TEST-OFFICER-001",
            tenantId: currentTenantId);
        SetVerificationStatus(officer, SharedKernel.Enums.VerificationStatus.Verified);

        var admin = new Person(
            firstName: "Test",
            lastName: "Admin",
            dateOfBirth: new DateOnly(1980, 3, 20),
            email: "admin@example.com",
            externalId: "TEST-ADMIN-001",
            tenantId: currentTenantId);
        SetVerificationStatus(admin, SharedKernel.Enums.VerificationStatus.Verified);

        // Additional test users for general testing
        var testUser = new Person(
            firstName: "Test",
            lastName: "User",
            dateOfBirth: new DateOnly(1995, 1, 1),
            email: "test@example.com",
            externalId: "TEST-USER-001",
            tenantId: currentTenantId);
        SetVerificationStatus(testUser, SharedKernel.Enums.VerificationStatus.Verified);

        var johnDoe = new Person(
            firstName: "John",
            lastName: "Doe",
            dateOfBirth: new DateOnly(1990, 5, 10),
            email: "john.doe@example.com",
            externalId: "TEST-USER-002",
            tenantId: currentTenantId);
        SetVerificationStatus(johnDoe, SharedKernel.Enums.VerificationStatus.Verified);

        // Additional users for authorization policy tests
        var multiRole = new Person(
            firstName: "Multi",
            lastName: "Role",
            dateOfBirth: new DateOnly(1988, 8, 8),
            email: "multirole@example.com",
            externalId: "TEST-MULTIROLE-001",
            tenantId: currentTenantId);
        SetVerificationStatus(multiRole, SharedKernel.Enums.VerificationStatus.Verified);

        var tenantA = new Person(
            firstName: "Tenant",
            lastName: "A User",
            dateOfBirth: new DateOnly(1992, 2, 2),
            email: "tenanta@example.com",
            externalId: "TEST-TENANTA-001",
            tenantId: currentTenantId);
        SetVerificationStatus(tenantA, SharedKernel.Enums.VerificationStatus.Verified);

        var testPersons = new[] { citizen, officer, admin, testUser, johnDoe, multiRole, tenantA };

        await context.Persons.AddRangeAsync(testPersons, cancellationToken);
        _logger.LogInformation("Seeded {Count} test persons for integration tests", testPersons.Length);
    }

    private void SetAuditFields(NumbatWalletDbContext context)
    {
        foreach (var entry in context.ChangeTracker.Entries())
        {
            if (entry.Entity is SharedKernel.Primitives.AuditableEntity<Guid>)
            {
                if (entry.State == EntityState.Added)
                {
                    entry.Property("CreatedAt").CurrentValue = DateTimeOffset.UtcNow;
                    entry.Property("CreatedBy").CurrentValue = "system";
                }
            }
        }
    }

    /// <summary>
    /// Helper method to set verification status on Person entity for test data seeding
    /// Uses reflection to bypass verification code validation flow
    /// </summary>
    private static void SetVerificationStatus(Person person, SharedKernel.Enums.VerificationStatus status)
    {
        var personType = person.GetType();

        // Set EmailVerificationStatus
        var emailField = personType.GetProperty("EmailVerificationStatus",
            System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
        if (emailField != null && emailField.CanWrite)
        {
            emailField.SetValue(person, status);
        }
        else
        {
            // Try backing field if property is read-only
            var emailBackingField = personType.GetField("<EmailVerificationStatus>k__BackingField",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            emailBackingField?.SetValue(person, status);
        }

        // Set PhoneVerificationStatus (required for IsVerified = true)
        var phoneField = personType.GetProperty("PhoneVerificationStatus",
            System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
        if (phoneField != null && phoneField.CanWrite)
        {
            phoneField.SetValue(person, status);
        }
        else
        {
            // Try backing field if property is read-only
            var phoneBackingField = personType.GetField("<PhoneVerificationStatus>k__BackingField",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            phoneBackingField?.SetValue(person, status);
        }
    }

    private async Task SeedTestWalletsAsync(NumbatWalletDbContext context, string currentTenantId, CancellationToken cancellationToken)
    {
        // Only seed in test environment
        var environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT");
        if (environment != "Testing" && environment != "Development")
        {
            return;
        }

        if (await context.Wallets.AnyAsync(cancellationToken))
        {
            _logger.LogInformation("Test wallets already seeded");
            return;
        }

        // Get test persons from database (they should be saved by now)
        var persons = await context.Persons.ToListAsync(cancellationToken);

        if (persons.Count == 0)
        {
            _logger.LogWarning("No persons found in database for wallet seeding. Ensure persons are seeded first.");
            return;
        }

        _logger.LogInformation("Found {Count} persons for wallet seeding", persons.Count);

        var wallets = new List<Wallet>();

        // Create wallets for each test person
        foreach (var person in persons)
        {
            var walletResult = Wallet.Create(
                person.Id,
                $"Test Wallet for {person.FirstName}");

            if (walletResult.IsSuccess)
            {
                walletResult.Value.SetTenantId(currentTenantId);
                wallets.Add(walletResult.Value);
                _logger.LogInformation("Created wallet for person {PersonId}: {WalletName}", person.Id, walletResult.Value.WalletName);
            }
            else
            {
                _logger.LogError("Failed to create wallet for person {PersonId}: {Error}", person.Id, walletResult.Error);
            }
        }

        // Also create some standalone test wallets for integration tests
        // We can't set specific GUIDs with the factory method, so just create more wallets
        for (int i = 0; i < 3; i++)
        {
            var walletResult = Wallet.Create(
                persons.First().Id,
                $"Test Integration Wallet {i + 1}");

            if (walletResult.IsSuccess)
            {
                walletResult.Value.SetTenantId(currentTenantId);
                wallets.Add(walletResult.Value);
            }
            else
            {
                _logger.LogError("Failed to create integration wallet {Index}: {Error}", i + 1, walletResult.Error);
            }
        }

        _logger.LogInformation("About to add {Count} wallets to context", wallets.Count);
        await context.Wallets.AddRangeAsync(wallets, cancellationToken);
        _logger.LogInformation("Seeded {Count} test wallets", wallets.Count);
    }

    public async Task SeedTestDataAsync(string? tenantId = null, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Seeding test data...");

        using var scope = _serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<NumbatWalletDbContext>();

        // Get tenant ID from parameter, or try to get from service, or use default
        var currentTenantId = tenantId;
        if (string.IsNullOrEmpty(currentTenantId))
        {
            var tenantService = scope.ServiceProvider.GetService<SharedKernel.Interfaces.ITenantService>();
            currentTenantId = tenantService?.TenantId.ToString() ?? "00000000-0000-0000-0000-000000000000";
        }
        _logger.LogInformation("Using tenant ID for test data: {TenantId}", currentTenantId);

        try
        {
            // Check if data already exists
            if (await context.Issuers.AnyAsync(cancellationToken))
            {
                _logger.LogInformation("Test data already exists, skipping seed");
                return;
            }

            // For tests, database should already be created with EnsureCreated
            // Just seed the data
            await SeedTenantsAsync(context, cancellationToken);
            await SeedIssuersAsync(context, currentTenantId, cancellationToken);

            // Set audit fields for initial batch
            SetAuditFields(context);
            await context.SaveChangesAsync(cancellationToken);

            // Seed persons and wallets in separate transaction
            await SeedTestPersonsAsync(context, currentTenantId, cancellationToken);
            SetAuditFields(context);
            await context.SaveChangesAsync(cancellationToken);

            await SeedTestWalletsAsync(context, currentTenantId, cancellationToken);
            SetAuditFields(context);
            await context.SaveChangesAsync(cancellationToken);
            _logger.LogInformation("Test data seeding completed successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred while seeding test data");
            throw;
        }
        finally
        {
            context.ChangeTracker.AutoDetectChangesEnabled = true;
        }
    }

    public async Task ClearAllDataAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogWarning("Clearing all data from database...");
        using var scope = _serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<NumbatWalletDbContext>();

        // Delete all data in reverse dependency order
        context.RemoveRange(context.Credentials);
        context.RemoveRange(context.Wallets);
        context.RemoveRange(context.Persons);
        context.RemoveRange(context.Issuers);
        context.RemoveRange(context.WalletTemplates);

        await context.SaveChangesAsync(cancellationToken);
        _logger.LogInformation("All data cleared from database");
    }

    public async Task SeedProductionDataAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Seeding production data...");
        using var scope = _serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<NumbatWalletDbContext>();

        // Check if already seeded
        if (await context.Issuers.AnyAsync(cancellationToken))
        {
            _logger.LogInformation("Production data already exists, skipping seed");
            return;
        }

        // Create default issuer for Western Australia Government
        var waGovIssuer = new Issuer(
            name: "Government of Western Australia",
            code: "WAGOV",
            issuerDid: "did:web:wa.gov.au",
            publicKey: "-----BEGIN PUBLIC KEY-----\nMIIBIjANBgkqhkiG9w0BAQEFAAOCAQ8AMIIBCgKCAQEA...\n-----END PUBLIC KEY-----",
            endpoint: "https://api.wa.gov.au/credentials",
            tenantId: "wa.gov.au"
        )
        {
            CreatedAt = DateTimeOffset.UtcNow,
            CreatedBy = "system"
        };
        waGovIssuer.SetTenantId("wa.gov.au");
        waGovIssuer.SetTrustedDomain("wa.gov.au");

        await context.Issuers.AddAsync(waGovIssuer, cancellationToken);
        await context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Production data seeded successfully");
    }
}

public static class DatabaseSeederExtensions
{
    public static async Task SeedDatabaseAsync(this IServiceProvider serviceProvider)
    {
        using var scope = serviceProvider.CreateScope();
        var seeder = scope.ServiceProvider.GetRequiredService<DatabaseSeeder>();
        await seeder.SeedAsync();
    }
}
