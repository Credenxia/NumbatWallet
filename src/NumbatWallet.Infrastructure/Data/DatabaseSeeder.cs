using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NumbatWallet.Domain.Aggregates;

namespace NumbatWallet.Infrastructure.Data;

public class DatabaseSeeder
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

        try
        {
            // Ensure database is created
            await context.Database.EnsureCreatedAsync(cancellationToken);

            // Run migrations if using them
            if (context.Database.IsRelational())
            {
                await context.Database.MigrateAsync(cancellationToken);
                _logger.LogInformation("Database migrations applied successfully");
            }

            // Seed data
            await SeedIssuersAsync(context, cancellationToken);
            await SeedTestPersonsAsync(context, cancellationToken);

            await context.SaveChangesAsync(cancellationToken);
            _logger.LogInformation("Database seeding completed successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred while seeding the database");
            throw;
        }
    }

    private async Task SeedIssuersAsync(NumbatWalletDbContext context, CancellationToken cancellationToken)
    {
        if (await context.Issuers.AnyAsync(cancellationToken))
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
            tenantId: "default");
        transportIssuer.UpdateDetails("Western Australia Department of Transport",
            "Official issuer for WA driving licenses and vehicle registrations");
        transportIssuer.MarkAsTrusted(10);
        transportIssuer.SetJurisdiction("Western Australia");
        transportIssuer.SetWebsiteUrl("https://www.transport.wa.gov.au");

        var healthIssuer = new Issuer(
            name: "Western Australia Department of Health",
            code: "WA-HEALTH",
            issuerDid: "did:web:health.wa.gov.au",
            publicKey: "MIIBIjANBgkqhkiG9w0BAQEFAAOCAQ8AMIIBCgKCAQEB...",
            endpoint: "https://api.health.wa.gov.au/credentials",
            tenantId: "default");
        healthIssuer.UpdateDetails("Western Australia Department of Health",
            "Official issuer for health and vaccination records");
        healthIssuer.MarkAsTrusted(10);
        healthIssuer.SetJurisdiction("Western Australia");
        healthIssuer.SetWebsiteUrl("https://www.health.wa.gov.au");

        var educationIssuer = new Issuer(
            name: "Western Australia Department of Education",
            code: "WA-EDU",
            issuerDid: "did:web:education.wa.gov.au",
            publicKey: "MIIBIjANBgkqhkiG9w0BAQEFAAOCAQ8AMIIBCgKCAQEC...",
            endpoint: "https://api.education.wa.gov.au/credentials",
            tenantId: "default");
        educationIssuer.UpdateDetails("Western Australia Department of Education",
            "Official issuer for educational credentials and certifications");
        educationIssuer.MarkAsTrusted(9);
        educationIssuer.SetJurisdiction("Western Australia");
        educationIssuer.SetWebsiteUrl("https://www.education.wa.gov.au");

        var issuers = new[] { transportIssuer, healthIssuer, educationIssuer };

        await context.Issuers.AddRangeAsync(issuers, cancellationToken);
        _logger.LogInformation("Seeded {Count} issuers", issuers.Length);
    }

    private async Task SeedTestPersonsAsync(NumbatWalletDbContext context, CancellationToken cancellationToken)
    {
        // Only seed in development environment
        var environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT");
        if (environment != "Development")
        {
            return;
        }

        if (await context.Persons.AnyAsync(cancellationToken))
        {
            _logger.LogInformation("Test persons already seeded");
            return;
        }

        // Create test persons
        var johnDoe = new Person(
            firstName: "John",
            lastName: "Doe",
            dateOfBirth: new DateOnly(1990, 1, 1),
            email: "john.doe@example.com",
            externalId: "TEST001",
            tenantId: "default");
        johnDoe.VerifyEmail("TEST-VERIFY-001");

        var janeSmith = new Person(
            firstName: "Jane",
            lastName: "Smith",
            dateOfBirth: new DateOnly(1985, 6, 15),
            email: "jane.smith@example.com",
            externalId: "TEST002",
            tenantId: "default");
        janeSmith.VerifyEmail("TEST-VERIFY-002");

        var bobJohnson = new Person(
            firstName: "Bob",
            lastName: "Johnson",
            dateOfBirth: new DateOnly(1995, 3, 20),
            email: "bob.johnson@example.com",
            externalId: "TEST003",
            tenantId: "default");
        // Bob's email remains pending

        var testPersons = new[] { johnDoe, janeSmith, bobJohnson };

        await context.Persons.AddRangeAsync(testPersons, cancellationToken);
        _logger.LogInformation("Seeded {Count} test persons", testPersons.Length);
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