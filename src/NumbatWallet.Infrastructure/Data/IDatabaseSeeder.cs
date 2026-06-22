namespace NumbatWallet.Infrastructure.Data;

/// <summary>
/// Interface for database seeding operations
/// </summary>
public interface IDatabaseSeeder
{
    /// <summary>
    /// Seeds the database with initial data
    /// </summary>
    Task SeedAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Seeds test data for development/testing environments
    /// </summary>
    Task SeedTestDataAsync(string? tenantId = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Clears all data from the database (testing only)
    /// </summary>
    Task ClearAllDataAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Seeds production data (initial setup)
    /// </summary>
    Task SeedProductionDataAsync(CancellationToken cancellationToken = default);
}