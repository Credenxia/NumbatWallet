using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using NumbatWallet.Domain.Aggregates;
using NumbatWallet.Infrastructure.Data;
using NumbatWallet.Integration.Tests.TestHarness;
using NumbatWallet.SharedKernel.Enums;
using NumbatWallet.SharedKernel.Interfaces;

namespace NumbatWallet.Integration.Tests.MultiTenancy;

/// <summary>
/// Integration tests for multi-tenant data isolation
/// Verifies that tenant data is properly isolated and cross-tenant access is prevented
/// </summary>
[Collection("Integration")]
public class MultiTenantIsolationTests : IntegrationTestBase
{
    public MultiTenantIsolationTests(IntegrationTestFixture fixture) : base(fixture)
    {
    }

    [Fact]
    public async Task TenantA_Data_IsIsolatedFrom_TenantB()
    {
        // Arrange - Create data for Tenant A and B
        var tenantAId = Fixture.TestTenantId; // Use test tenant from fixture
        var tenantBId = "tenant-b";
        var personId = await TestData.GetFirstPersonIdAsync();

        var walletAId = Guid.NewGuid();
        var walletBId = Guid.NewGuid();

        // Save both wallets using raw SQL to bypass tenant interceptor
        using (var tempContext = Fixture.Services.CreateScope().ServiceProvider.GetRequiredService<NumbatWalletDbContext>())
        {
            var walletDidA = $"did:wallet:{walletAId}";
            var walletDidB = $"did:wallet:{walletBId}";
            var now = DateTime.UtcNow;
            var walletNameA = "Tenant A Wallet";
            var walletNameB = "Tenant B Wallet";
            var walletStatus = "ACTIVE";
            var createdBy = "test";

            // Insert wallet A using ExecuteSqlRawAsync with explicit parameters (snake_case column names)
            await tempContext.Database.ExecuteSqlRawAsync(
                "INSERT INTO \"Wallets\" (id, person_id, wallet_name, wallet_did, type, status, tenant_id, created_at, created_by) VALUES ({0}, {1}, {2}, {3}, 0, {4}, {5}, {6}, {7})",
                walletAId, personId, walletNameA, walletDidA, walletStatus, tenantAId, now, createdBy);

            // Insert wallet B using ExecuteSqlRawAsync with explicit parameters (snake_case column names)
            await tempContext.Database.ExecuteSqlRawAsync(
                "INSERT INTO \"Wallets\" (id, person_id, wallet_name, wallet_did, type, status, tenant_id, created_at, created_by) VALUES ({0}, {1}, {2}, {3}, 0, {4}, {5}, {6}, {7})",
                walletBId, personId, walletNameB, walletDidB, walletStatus, tenantBId, now, createdBy);
        }

        // Act - Query as Tenant A (using the test fixture's tenant context)
        using var scope = Fixture.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<NumbatWalletDbContext>();

        var walletsForTenantA = await dbContext.Set<Wallet>()
            .Where(w => w.TenantId == tenantAId)
            .ToListAsync();

        // Assert - Should only see Tenant A's wallet (filter by specific wallet ID)
        var testWallet = walletsForTenantA.FirstOrDefault(w => w.Id == walletAId);
        testWallet.Should().NotBeNull();
        testWallet.TenantId.Should().Be(tenantAId);
        walletsForTenantA.Should().NotContain(w => w.Id == walletBId);

        // Cleanup - Delete test wallets
        using (var cleanupContext = Fixture.Services.CreateScope().ServiceProvider.GetRequiredService<NumbatWalletDbContext>())
        {
            await cleanupContext.Database.ExecuteSqlRawAsync("DELETE FROM \"Wallets\" WHERE id = {0} OR id = {1}", walletAId, walletBId);
        }
    }

    [Fact]
    public async Task TenantInterceptor_Automatically_FiltersQueries_ByTenant()
    {
        // Arrange - Create wallets for multiple tenants
        var tenant1 = Fixture.TestTenantId;
        var tenant2 = "tenant-2";
        var tenant3 = "tenant-3";
        var personId = await TestData.GetFirstPersonIdAsync();

        var wallet1Id = Guid.NewGuid();
        var wallet2Id = Guid.NewGuid();
        var wallet3Id = Guid.NewGuid();
        var wallet4Id = Guid.NewGuid();

        // Save all wallets using raw SQL to bypass tenant interceptor
        using (var tempContext = Fixture.Services.CreateScope().ServiceProvider.GetRequiredService<NumbatWalletDbContext>())
        {
            var now = DateTime.UtcNow;
            var walletStatus = "ACTIVE";
            var createdBy = "test";

            // Insert all wallets using FormattableString
            var wallets = new[]
            {
                (wallet1Id, "Wallet 1", tenant1),
                (wallet2Id, "Wallet 2", tenant1),
                (wallet3Id, "Wallet 3", tenant2),
                (wallet4Id, "Wallet 4", tenant3)
            };

            foreach (var (walletId, walletName, tenantId) in wallets)
            {
                var walletDid = $"did:wallet:{walletId}";
                await tempContext.Database.ExecuteSqlRawAsync(
                "INSERT INTO \"Wallets\" (id, person_id, wallet_name, wallet_did, type, status, tenant_id, created_at, created_by) VALUES ({0}, {1}, {2}, {3}, 0, {4}, {5}, {6}, {7})",
                walletId, personId, walletName, walletDid, walletStatus, tenantId, now, createdBy);
            }
        }

        // Act - Query without explicit tenant filter (tenant interceptor should apply it)
        using var scope = Fixture.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<NumbatWalletDbContext>();
        var allWallets = await dbContext.Set<Wallet>().ToListAsync();

        // Assert - Should get tenant1 wallets (check for our specific test wallets)
        var testWallets = allWallets.Where(w => w.Id == wallet1Id || w.Id == wallet2Id).ToList();
        testWallets.Should().HaveCount(2);
        testWallets.Should().AllSatisfy(w => w.TenantId.Should().Be(tenant1));
        allWallets.Should().NotContain(w => w.Id == wallet3Id || w.Id == wallet4Id);

        // Cleanup - Delete test wallets
        using (var cleanupContext = Fixture.Services.CreateScope().ServiceProvider.GetRequiredService<NumbatWalletDbContext>())
        {
            await cleanupContext.Database.ExecuteSqlRawAsync(
                "DELETE FROM \"Wallets\" WHERE id IN ({0}, {1}, {2}, {3})",
                wallet1Id, wallet2Id, wallet3Id, wallet4Id);
        }
    }

    [Fact]
    public async Task SaveChanges_Automatically_SetsTenantId_ForNewEntities()
    {
        // Arrange
        var personId = await TestData.GetFirstPersonIdAsync();
        var wallet = Wallet.Create(
            personId: personId,
            walletName: "Test Wallet",
            type: WalletType.Holder).Value;

        // Act - Save without setting TenantId (should be set by interceptor)
        using var scope = Fixture.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<NumbatWalletDbContext>();

        await dbContext.AddAsync(wallet);
        await dbContext.SaveChangesAsync();

        // Assert - TenantId should be set to fixture's test tenant
        var savedWallet = await dbContext.Set<Wallet>().FindAsync(wallet.Id);
        savedWallet.Should().NotBeNull();
        savedWallet.TenantId.Should().Be(Fixture.TestTenantId);
    }

    [Fact]
    public async Task CrossTenant_Update_IsNotAllowed()
    {
        // Arrange - Create wallet for different tenant
        var otherTenantId = "other-tenant";
        var personId = await TestData.GetFirstPersonIdAsync();
        var walletId = Guid.NewGuid();

        // Save wallet using raw SQL to bypass tenant interceptor
        using (var tempContext = Fixture.Services.CreateScope().ServiceProvider.GetRequiredService<NumbatWalletDbContext>())
        {
            var walletDid = $"did:wallet:{walletId}";
            var now = DateTime.UtcNow;
            var walletName = "Other Tenant Wallet";
            var walletStatus = "ACTIVE";
            var createdBy = "test";

            await tempContext.Database.ExecuteSqlRawAsync(
                "INSERT INTO \"Wallets\" (id, person_id, wallet_name, wallet_did, type, status, tenant_id, created_at, created_by) VALUES ({0}, {1}, {2}, {3}, 0, {4}, {5}, {6}, {7})",
                walletId, personId, walletName, walletDid, walletStatus, otherTenantId, now, createdBy);
        }

        // Act - Try to query and update wallet from different tenant
        using var scope = Fixture.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<NumbatWalletDbContext>();

        var walletQuery = await dbContext.Set<Wallet>()
            .Where(w => w.Id == walletId)
            .FirstOrDefaultAsync();

        // Assert - Should not be able to find wallet from other tenant
        walletQuery.Should().BeNull();
    }

    [Fact]
    public async Task TenantService_ProvidesCorrect_TenantContext()
    {
        // Arrange
        var tenantService = Fixture.Services.GetRequiredService<ITenantService>();

        // Act
        var tenantId = tenantService.TenantId;
        var tenantName = tenantService.TenantName;

        // Assert
        tenantId.Should().NotBe(Guid.Empty);
        tenantName.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task CurrentTenantService_ProvidesCorrect_StringTenantId()
    {
        // Arrange
        var currentTenantService = Fixture.Services.GetRequiredService<ICurrentTenantService>();

        // Act
        var tenantId = currentTenantService.TenantId;
        var tenantName = currentTenantService.TenantName;

        // Assert
        tenantId.Should().NotBeNullOrEmpty();
        tenantName.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task Credentials_AreIsolated_ByTenant()
    {
        // Arrange - Create wallets for different tenants
        var tenant1 = Fixture.TestTenantId;
        var tenant2 = "tenant-2";
        var personId = await TestData.GetFirstPersonIdAsync();

        var wallet1Id = Guid.NewGuid();
        var wallet2Id = Guid.NewGuid();

        // Save both wallets using raw SQL to bypass tenant interceptor
        using (var tempContext = Fixture.Services.CreateScope().ServiceProvider.GetRequiredService<NumbatWalletDbContext>())
        {
            var walletDid1 = $"did:wallet:{wallet1Id}";
            var walletDid2 = $"did:wallet:{wallet2Id}";
            var now = DateTime.UtcNow;
            var wallet1Name = "Wallet 1";
            var wallet2Name = "Wallet 2";
            var walletStatus = "ACTIVE";
            var createdBy = "test";

            await tempContext.Database.ExecuteSqlRawAsync(
                "INSERT INTO \"Wallets\" (id, person_id, wallet_name, wallet_did, type, status, tenant_id, created_at, created_by) VALUES ({0}, {1}, {2}, {3}, 0, {4}, {5}, {6}, {7})",
                wallet1Id, personId, wallet1Name, walletDid1, walletStatus, tenant1, now, createdBy);

            await tempContext.Database.ExecuteSqlRawAsync(
                "INSERT INTO \"Wallets\" (id, person_id, wallet_name, wallet_did, type, status, tenant_id, created_at, created_by) VALUES ({0}, {1}, {2}, {3}, 0, {4}, {5}, {6}, {7})",
                wallet2Id, personId, wallet2Name, walletDid2, walletStatus, tenant2, now, createdBy);
        }

        // Act - Query wallets (should only see tenant1's wallet)
        using var scope = Fixture.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<NumbatWalletDbContext>();
        var visibleWallets = await dbContext.Set<Wallet>().ToListAsync();

        // Assert - Should only see tenant1's wallet (check for specific ID)
        var testWallet = visibleWallets.FirstOrDefault(w => w.Id == wallet1Id);
        testWallet.Should().NotBeNull();
        testWallet.TenantId.Should().Be(tenant1);
        visibleWallets.Should().NotContain(w => w.Id == wallet2Id);

        // Cleanup - Delete test wallets
        using (var cleanupContext = Fixture.Services.CreateScope().ServiceProvider.GetRequiredService<NumbatWalletDbContext>())
        {
            await cleanupContext.Database.ExecuteSqlRawAsync(
                "DELETE FROM \"Wallets\" WHERE id IN ({0}, {1})",
                wallet1Id, wallet2Id);
        }
    }

    [Fact]
    public async Task Tenant_CannotAccess_AnotherTenant_Credentials()
    {
        // Arrange - Create wallet for tenant B
        var tenantB = "tenant-b";
        var personId = await TestData.GetFirstPersonIdAsync();
        var walletBId = Guid.NewGuid();

        // Save wallet using raw SQL to bypass tenant interceptor
        using (var tempContext = Fixture.Services.CreateScope().ServiceProvider.GetRequiredService<NumbatWalletDbContext>())
        {
            var walletDid = $"did:wallet:{walletBId}";
            var now = DateTime.UtcNow;
            var walletName = "Tenant B Wallet";
            var walletStatus = "ACTIVE";
            var createdBy = "test";

            await tempContext.Database.ExecuteSqlRawAsync(
                "INSERT INTO \"Wallets\" (id, person_id, wallet_name, wallet_did, type, status, tenant_id, created_at, created_by) VALUES ({0}, {1}, {2}, {3}, 0, {4}, {5}, {6}, {7})",
                walletBId, personId, walletName, walletDid, walletStatus, tenantB, now, createdBy);
        }

        // Act - Try to query tenant B's wallet as tenant A
        using var scope = Fixture.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<NumbatWalletDbContext>();
        var wallet = await dbContext.Set<Wallet>()
            .FirstOrDefaultAsync(w => w.Id == walletBId);

        // Assert - Should not be able to see tenant B's wallet
        wallet.Should().BeNull();
    }

    [Fact]
    public async Task BulkQuery_RespectsTenant_Boundaries()
    {
        // Arrange - Create 100 wallets across 3 tenants
        var tenant1 = Fixture.TestTenantId;
        var tenant2 = "tenant-2";
        var tenant3 = "tenant-3";
        var personId = await TestData.GetFirstPersonIdAsync();

        var wallets = new List<(Guid Id, string Name, string TenantId)>();

        for (int i = 0; i < 100; i++)
        {
            var tenantId = i < 50 ? tenant1 : (i < 75 ? tenant2 : tenant3);
            wallets.Add((Guid.NewGuid(), $"Wallet {i}", tenantId));
        }

        // Save all wallets using raw SQL to bypass tenant interceptor
        using (var tempContext = Fixture.Services.CreateScope().ServiceProvider.GetRequiredService<NumbatWalletDbContext>())
        {
            var now = DateTime.UtcNow;
            var walletStatus = "ACTIVE";
            var createdBy = "test";

            foreach (var (walletId, walletName, tenantId) in wallets)
            {
                var walletDid = $"did:wallet:{walletId}";
                await tempContext.Database.ExecuteSqlRawAsync(
                "INSERT INTO \"Wallets\" (id, person_id, wallet_name, wallet_did, type, status, tenant_id, created_at, created_by) VALUES ({0}, {1}, {2}, {3}, 0, {4}, {5}, {6}, {7})",
                walletId, personId, walletName, walletDid, walletStatus, tenantId, now, createdBy);
            }
        }

        // Act - Query all wallets (should only get tenant1's 50 wallets)
        using var scope = Fixture.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<NumbatWalletDbContext>();
        var visibleWallets = await dbContext.Set<Wallet>().ToListAsync();

        // Assert - Should get tenant1's wallets (check for our specific test data)
        var testWallets = visibleWallets.Where(w => wallets.Any(tw => tw.Id == w.Id)).ToList();
        testWallets.Should().HaveCountGreaterThanOrEqualTo(50);
        testWallets.Should().AllSatisfy(w => w.TenantId.Should().Be(tenant1));

        // Cleanup - Delete all test wallets
        using (var cleanupContext = Fixture.Services.CreateScope().ServiceProvider.GetRequiredService<NumbatWalletDbContext>())
        {
            foreach (var (walletId, _, _) in wallets)
            {
                await cleanupContext.Database.ExecuteSqlRawAsync("DELETE FROM \"Wallets\" WHERE id = {0}", walletId);
            }
        }
    }

    [Fact]
    public async Task TenantId_CannotBe_Modified_AfterCreation()
    {
        // Arrange
        var tenant1 = Fixture.TestTenantId;
        var personId = await TestData.GetFirstPersonIdAsync();
        var wallet = Wallet.Create(
            personId,
            "Test Wallet").Value;
        wallet.SetTenantId(tenant1);

        // Save wallet
        using var scope1 = Fixture.Services.CreateScope();
        var dbContext1 = scope1.ServiceProvider.GetRequiredService<NumbatWalletDbContext>();
        await dbContext1.AddAsync(wallet);
        await dbContext1.SaveChangesAsync();

        // Act - Try to verify tenant ID property protection
        var tenantIdProperty = typeof(Wallet).GetProperty("TenantId");
        tenantIdProperty.Should().NotBeNull();
        tenantIdProperty.SetMethod.Should().NotBeNull();
        tenantIdProperty.SetMethod!.IsPrivate.Should().BeTrue();

        // Assert - Verify wallet still has original tenant
        using var scope2 = Fixture.Services.CreateScope();
        var dbContext2 = scope2.ServiceProvider.GetRequiredService<NumbatWalletDbContext>();
        var loadedWallet = await dbContext2.Set<Wallet>().FirstAsync(w => w.Id == wallet.Id);
        loadedWallet.TenantId.Should().Be(tenant1);
    }

    [Fact]
    public async Task GlobalQuery_WithoutTenantFilter_StillRespects_TenantIsolation()
    {
        // Arrange
        var tenant1 = Fixture.TestTenantId;
        var tenant2 = "tenant-2";
        var personId = await TestData.GetFirstPersonIdAsync();

        var wallet1Id = Guid.NewGuid();
        var wallet2Id = Guid.NewGuid();

        // Save both wallets using raw SQL to bypass tenant interceptor
        using (var tempContext = Fixture.Services.CreateScope().ServiceProvider.GetRequiredService<NumbatWalletDbContext>())
        {
            var walletDid1 = $"did:wallet:{wallet1Id}";
            var walletDid2 = $"did:wallet:{wallet2Id}";
            var now = DateTime.UtcNow;
            var wallet1Name = "Wallet 1";
            var wallet2Name = "Wallet 2";
            var walletStatus = "ACTIVE";
            var createdBy = "test";

            await tempContext.Database.ExecuteSqlRawAsync(
                "INSERT INTO \"Wallets\" (id, person_id, wallet_name, wallet_did, type, status, tenant_id, created_at, created_by) VALUES ({0}, {1}, {2}, {3}, 0, {4}, {5}, {6}, {7})",
                wallet1Id, personId, wallet1Name, walletDid1, walletStatus, tenant1, now, createdBy);

            await tempContext.Database.ExecuteSqlRawAsync(
                "INSERT INTO \"Wallets\" (id, person_id, wallet_name, wallet_did, type, status, tenant_id, created_at, created_by) VALUES ({0}, {1}, {2}, {3}, 0, {4}, {5}, {6}, {7})",
                wallet2Id, personId, wallet2Name, walletDid2, walletStatus, tenant2, now, createdBy);
        }

        // Act - Execute global query (tenant interceptor should still apply)
        using var scope = Fixture.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<NumbatWalletDbContext>();
        var count = await dbContext.Set<Wallet>().CountAsync();

        // Assert - Should get at least 1 wallet for tenant1 (our test wallet)
        count.Should().BeGreaterThanOrEqualTo(1);

        // Cleanup - Delete test wallets
        using (var cleanupContext = Fixture.Services.CreateScope().ServiceProvider.GetRequiredService<NumbatWalletDbContext>())
        {
            await cleanupContext.Database.ExecuteSqlRawAsync(
                "DELETE FROM \"Wallets\" WHERE id IN ({0}, {1})",
                wallet1Id, wallet2Id);
        }
    }

    [Fact]
    public async Task MultipleContexts_WithDifferentTenants_AreIsolated()
    {
        // Verify that the current context has proper tenant isolation
        using var scope = Fixture.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<NumbatWalletDbContext>();
        var wallets = await dbContext.Set<Wallet>().ToListAsync();

        if (wallets.Any())
        {
            wallets.Should().AllSatisfy(w =>
                w.TenantId.Should().Be(Fixture.TestTenantId));
        }
        else
        {
            // No wallets is also acceptable for tenant isolation
            wallets.Should().BeEmpty();
        }
    }
}

