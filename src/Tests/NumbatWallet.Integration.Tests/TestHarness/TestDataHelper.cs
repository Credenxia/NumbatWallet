using NumbatWallet.Infrastructure.Data;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.EntityFrameworkCore;

namespace NumbatWallet.Integration.Tests.TestHarness;

/// <summary>
/// Helper class to provide access to seeded test data IDs
/// </summary>
public class TestDataHelper
{
    private readonly IServiceProvider _serviceProvider;

    public TestDataHelper(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    public async Task<Guid> GetFirstWalletIdAsync()
    {
        using var scope = _serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<NumbatWalletDbContext>();

        // Debug: Check what data exists
        var personCount = await context.Persons.CountAsync();
        var issuerCount = await context.Issuers.CountAsync();
        var walletCount = await context.Wallets.CountAsync();

        var wallet = await context.Wallets.FirstOrDefaultAsync();
        if (wallet == null)
        {
            throw new InvalidOperationException(
                $"No wallets found in test database. Database stats: " +
                $"Persons={personCount}, Issuers={issuerCount}, Wallets={walletCount}. " +
                $"Ensure seed data is loaded.");
        }

        return wallet.Id;
    }

    public async Task<Guid> GetFirstIssuerIdAsync()
    {
        using var scope = _serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<NumbatWalletDbContext>();

        var issuer = await context.Issuers.FirstOrDefaultAsync();
        if (issuer == null)
        {
            throw new InvalidOperationException("No issuers found in test database. Ensure seed data is loaded.");
        }

        return issuer.Id;
    }

    public async Task<Guid> GetFirstPersonIdAsync()
    {
        using var scope = _serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<NumbatWalletDbContext>();

        var person = await context.Persons.FirstOrDefaultAsync();
        if (person == null)
        {
            throw new InvalidOperationException("No persons found in test database. Ensure seed data is loaded.");
        }

        return person.Id;
    }

    public async Task<List<Guid>> GetAllWalletIdsAsync()
    {
        using var scope = _serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<NumbatWalletDbContext>();

        return await context.Wallets.Select(w => w.Id).ToListAsync();
    }

    public async Task<List<Guid>> GetAllIssuerIdsAsync()
    {
        using var scope = _serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<NumbatWalletDbContext>();

        return await context.Issuers.Select(i => i.Id).ToListAsync();
    }
}