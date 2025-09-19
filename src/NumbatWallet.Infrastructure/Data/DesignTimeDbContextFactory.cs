using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using NumbatWallet.SharedKernel.Interfaces;
using Moq;

namespace NumbatWallet.Infrastructure.Data;

/// <summary>
/// Factory for creating DbContext at design time for EF Core tools
/// </summary>
public class DesignTimeDbContextFactory : IDesignTimeDbContextFactory<NumbatWalletDbContext>
{
    public NumbatWalletDbContext CreateDbContext(string[] args)
    {
        // Build configuration
        var configuration = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json", optional: true)
            .AddJsonFile("appsettings.Development.json", optional: true)
            .AddEnvironmentVariables()
            .Build();

        // Create DbContext options
        var optionsBuilder = new DbContextOptionsBuilder<NumbatWalletDbContext>();

        // Use a default connection string for migrations
        var connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? "Host=localhost;Database=numbatwallet_dev;Username=postgres;Password=postgres";

        optionsBuilder.UseNpgsql(connectionString, options =>
        {
            options.MigrationsAssembly("NumbatWallet.Infrastructure");
        });

        // Create mock services for design time
        var tenantServiceMock = new Mock<ITenantService>();
        tenantServiceMock.Setup(x => x.TenantId).Returns(Guid.NewGuid());

        var currentUserServiceMock = new Mock<ICurrentUserService>();
        currentUserServiceMock.Setup(x => x.UserId).Returns("design-time-user");

        var dateTimeServiceMock = new Mock<IDateTimeService>();
        dateTimeServiceMock.Setup(x => x.UtcNow).Returns(DateTimeOffset.UtcNow);

        var eventDispatcherMock = new Mock<IEventDispatcher>();

        var loggerMock = new Mock<ILogger<NumbatWalletDbContext>>();

        // Create DbContext instance
        return new NumbatWalletDbContext(
            optionsBuilder.Options,
            tenantServiceMock.Object,
            currentUserServiceMock.Object,
            dateTimeServiceMock.Object,
            eventDispatcherMock.Object,
            loggerMock.Object
        );
    }
}