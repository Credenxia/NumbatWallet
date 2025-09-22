using FluentAssertions;
using HotChocolate.Execution;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using NumbatWallet.Application.DTOs;
using NumbatWallet.Application.Interfaces;
using NumbatWallet.Web.Api.GraphQL.Admin;
using Xunit;

namespace NumbatWallet.Web.Api.Tests.GraphQL.Admin;

/// <summary>
/// Integration tests for Admin GraphQL queries
/// POA: Ensuring high quality and security for admin operations
/// </summary>
public class AdminQueryTests : GraphQLTestBase
{
    private readonly Mock<IHealthCheckService> _healthServiceMock;
    private readonly Mock<IStatisticsService> _statisticsServiceMock;
    private readonly Mock<ITenantService> _tenantServiceMock;
    private readonly Mock<IBackupService> _backupServiceMock;
    private readonly Mock<IFeatureFlagService> _featureFlagServiceMock;

    public AdminQueryTests()
    {
        _healthServiceMock = new Mock<IHealthCheckService>();
        _statisticsServiceMock = new Mock<IStatisticsService>();
        _tenantServiceMock = new Mock<ITenantService>();
        _backupServiceMock = new Mock<IBackupService>();
        _featureFlagServiceMock = new Mock<IFeatureFlagService>();
    }

    protected override void ConfigureServices(IServiceCollection services)
    {
        base.ConfigureServices(services);

        services.AddSingleton(_healthServiceMock.Object);
        services.AddSingleton(_statisticsServiceMock.Object);
        services.AddSingleton(_tenantServiceMock.Object);
        services.AddSingleton(_backupServiceMock.Object);
        services.AddSingleton(_featureFlagServiceMock.Object);
    }

    [Fact]
    public async Task GetSystemHealth_Should_Return_Health_Status()
    {
        // Arrange
        var expectedHealth = new SystemHealthDto
        {
            Status = "Healthy",
            Components = new Dictionary<string, ComponentHealthDto>
            {
                ["Database"] = new() { Name = "Database", Status = "Healthy" },
                ["Redis"] = new() { Name = "Redis", Status = "Healthy" }
            },
            CheckedAt = DateTime.UtcNow
        };

        _healthServiceMock
            .Setup(x => x.GetSystemHealthAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedHealth);

        var query = @"
            query {
                systemHealth {
                    status
                    components
                    checkedAt
                }
            }";

        // Act
        var result = await ExecuteQueryAsync(query, isAdmin: true);

        // Assert
        result.Errors.Should().BeNullOrEmpty();
        var data = result.Data?.ToDictionary();
        data.Should().ContainKey("systemHealth");

        var health = data["systemHealth"] as Dictionary<string, object>;
        health["status"].Should().Be("Healthy");
    }

    [Fact]
    public async Task GetSystemHealth_Should_Require_Admin_Authorization()
    {
        // Arrange
        var query = @"
            query {
                systemHealth {
                    status
                }
            }";

        // Act
        var result = await ExecuteQueryAsync(query, isAdmin: false);

        // Assert
        result.Errors.Should().NotBeNullOrEmpty();
        result.Errors[0].Message.Should().Contain("authorization");
    }

    [Fact]
    public async Task GetMetrics_Should_Return_Metrics_For_TimeRange()
    {
        // Arrange
        var expectedMetrics = new MetricsSnapshotDto
        {
            From = DateTime.UtcNow.AddHours(-24),
            To = DateTime.UtcNow,
            Metrics = new Dictionary<string, decimal>
            {
                ["TotalRequests"] = 10000,
                ["AverageResponseTime"] = 150.5m,
                ["ErrorRate"] = 0.02m
            }
        };

        _statisticsServiceMock
            .Setup(x => x.GetMetricsSnapshotAsync(
                It.IsAny<DateTime>(),
                It.IsAny<DateTime>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedMetrics);

        var query = @"
            query {
                metrics(timeRange: { type: LAST_24_HOURS }) {
                    from
                    to
                    metrics
                }
            }";

        // Act
        var result = await ExecuteQueryAsync(query, isAdmin: true);

        // Assert
        result.Errors.Should().BeNullOrEmpty();
        var data = result.Data?.ToDictionary();
        data.Should().ContainKey("metrics");
    }

    [Fact]
    public async Task GetTenants_Should_Return_Filtered_Tenants()
    {
        // Arrange
        var tenants = new List<TenantDto>
        {
            new() { Id = "1", Name = "Tenant A", Status = TenantStatus.Active },
            new() { Id = "2", Name = "Tenant B", Status = TenantStatus.Active }
        };

        _tenantServiceMock
            .Setup(x => x.GetAllTenants())
            .Returns(tenants.AsQueryable());

        var query = @"
            query {
                tenants(filter: { status: ACTIVE }) {
                    nodes {
                        id
                        name
                        status
                    }
                }
            }";

        // Act
        var result = await ExecuteQueryAsync(query, isAdmin: true);

        // Assert
        result.Errors.Should().BeNullOrEmpty();
        var data = result.Data?.ToDictionary();
        data.Should().ContainKey("tenants");
    }

    [Fact]
    public async Task GetTenant_Should_Return_Specific_Tenant()
    {
        // Arrange
        var expectedTenant = new TenantDto
        {
            Id = "123",
            Name = "Test Tenant",
            Identifier = "test-tenant",
            Status = TenantStatus.Active
        };

        _tenantServiceMock
            .Setup(x => x.GetTenantByIdAsync("123", It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedTenant);

        var query = @"
            query {
                tenant(id: ""123"") {
                    id
                    name
                    identifier
                    status
                }
            }";

        // Act
        var result = await ExecuteQueryAsync(query, isAdmin: true);

        // Assert
        result.Errors.Should().BeNullOrEmpty();
        var data = result.Data?.ToDictionary();
        data.Should().ContainKey("tenant");

        var tenant = data["tenant"] as Dictionary<string, object>;
        tenant["id"].Should().Be("123");
        tenant["name"].Should().Be("Test Tenant");
    }

    [Fact]
    public async Task GetFeatureFlags_Should_Return_All_Flags()
    {
        // Arrange
        var flags = new List<FeatureFlagDto>
        {
            new() { Id = "1", Name = "NewFeature", IsEnabled = true },
            new() { Id = "2", Name = "BetaFeature", IsEnabled = false }
        };

        _featureFlagServiceMock
            .Setup(x => x.GetAllFlagsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(flags);

        var query = @"
            query {
                featureFlags {
                    id
                    name
                    isEnabled
                }
            }";

        // Act
        var result = await ExecuteQueryAsync(query, isAdmin: true);

        // Assert
        result.Errors.Should().BeNullOrEmpty();
        var data = result.Data?.ToDictionary();
        data.Should().ContainKey("featureFlags");

        var resultFlags = data["featureFlags"] as List<object>;
        resultFlags.Should().HaveCount(2);
    }

    [Fact]
    public async Task GetBackups_Should_Return_Backup_History()
    {
        // Arrange
        var backups = new List<BackupDto>
        {
            new()
            {
                Id = "1",
                Type = "Full",
                Status = "Completed",
                SizeBytes = 1024000,
                CreatedAt = DateTime.UtcNow.AddDays(-1)
            }
        };

        _backupServiceMock
            .Setup(x => x.GetBackupHistoryAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(backups);

        var query = @"
            query {
                backups {
                    nodes {
                        id
                        type
                        status
                        sizeBytes
                        createdAt
                    }
                }
            }";

        // Act
        var result = await ExecuteQueryAsync(query, isAdmin: true);

        // Assert
        result.Errors.Should().BeNullOrEmpty();
        var data = result.Data?.ToDictionary();
        data.Should().ContainKey("backups");
    }

    [Theory]
    [InlineData("USAGE", ReportType.Usage)]
    [InlineData("SECURITY", ReportType.Security)]
    [InlineData("COMPLIANCE", ReportType.Compliance)]
    public async Task GenerateReport_Should_Create_Report_Of_Type(string reportTypeStr, ReportType expectedType)
    {
        // Arrange
        var expectedReport = new ReportDto
        {
            Id = "report-123",
            Type = expectedType,
            Format = "PDF",
            Content = new byte[] { 1, 2, 3 },
            GeneratedAt = DateTime.UtcNow
        };

        _reportingServiceMock
            .Setup(x => x.GenerateReportAsync(
                expectedType,
                It.IsAny<ReportParameters>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedReport);

        var query = $@"
            query {{
                generateReport(
                    type: {reportTypeStr},
                    parameters: {{ format: ""PDF"" }}
                ) {{
                    id
                    type
                    format
                    generatedAt
                }}
            }}";

        // Act
        var result = await ExecuteQueryAsync(query, isAdmin: true);

        // Assert
        result.Errors.Should().BeNullOrEmpty();
        var data = result.Data?.ToDictionary();
        data.Should().ContainKey("generateReport");

        var report = data["generateReport"] as Dictionary<string, object>;
        report["type"].Should().Be(expectedType.ToString());
    }

    [Fact]
    public async Task Query_With_Pagination_Should_Return_Paged_Results()
    {
        // Arrange
        var tenants = Enumerable.Range(1, 50)
            .Select(i => new TenantDto
            {
                Id = i.ToString(),
                Name = $"Tenant {i}",
                Status = TenantStatus.Active
            })
            .ToList();

        _tenantServiceMock
            .Setup(x => x.GetAllTenants())
            .Returns(tenants.AsQueryable());

        var query = @"
            query {
                tenants(first: 10) {
                    pageInfo {
                        hasNextPage
                        hasPreviousPage
                    }
                    nodes {
                        id
                        name
                    }
                    totalCount
                }
            }";

        // Act
        var result = await ExecuteQueryAsync(query, isAdmin: true);

        // Assert
        result.Errors.Should().BeNullOrEmpty();
        var data = result.Data?.ToDictionary();
        data.Should().ContainKey("tenants");

        var tenantsResult = data["tenants"] as Dictionary<string, object>;
        var pageInfo = tenantsResult["pageInfo"] as Dictionary<string, object>;
        pageInfo["hasNextPage"].Should().Be(true);

        var nodes = tenantsResult["nodes"] as List<object>;
        nodes.Should().HaveCount(10);
    }
}

/// <summary>
/// Base class for GraphQL tests
/// </summary>
public abstract class GraphQLTestBase : IDisposable
{
    private readonly ServiceProvider _serviceProvider;
    protected readonly IRequestExecutor Executor;

    protected GraphQLTestBase()
    {
        var services = new ServiceCollection();
        ConfigureServices(services);

        services
            .AddGraphQLServer()
            .AddQueryType<Query>()
            .AddMutationType<Mutation>()
            .AddSubscriptionType<Subscription>()
            .AddTypeExtension<AdminQuery>()
            .AddTypeExtension<AdminMutation>()
            .AddTypeExtension<AdminSubscription>()
            .AddAuthorization()
            .AddFiltering()
            .AddSorting()
            .AddProjections();

        _serviceProvider = services.BuildServiceProvider();
        Executor = _serviceProvider.GetRequiredService<IRequestExecutorResolver>()
            .GetRequestExecutorAsync().Result;
    }

    protected virtual void ConfigureServices(IServiceCollection services)
    {
        // Add mock services
        services.AddSingleton<IReportingService>(new Mock<IReportingService>().Object);
        services.AddSingleton<IConfigurationService>(new Mock<IConfigurationService>().Object);
        services.AddSingleton<IUserManagementService>(new Mock<IUserManagementService>().Object);
        services.AddSingleton<IKeyManagementService>(new Mock<IKeyManagementService>().Object);
        services.AddSingleton<IMaintenanceService>(new Mock<IMaintenanceService>().Object);
        services.AddSingleton<ICacheService>(new Mock<ICacheService>().Object);

        // Add authorization
        services.AddAuthorization(options =>
        {
            options.AddPolicy("AdminOnly", policy => policy.RequireRole("Admin"));
            options.AddPolicy("SuperAdmin", policy => policy.RequireRole("SuperAdmin"));
        });
    }

    protected async Task<IExecutionResult> ExecuteQueryAsync(
        string query,
        bool isAdmin = false,
        Dictionary<string, object?>? variables = null)
    {
        var requestBuilder = QueryRequestBuilder.New()
            .SetQuery(query);

        if (variables != null)
        {
            requestBuilder.SetVariables(variables);
        }

        if (isAdmin)
        {
            requestBuilder.AddProperty("IsAdmin", true);
        }

        return await Executor.ExecuteAsync(requestBuilder.Create());
    }

    public void Dispose()
    {
        _serviceProvider?.Dispose();
    }
}

// Mock interfaces that don't exist yet
public interface IReportingService
{
    Task<ReportDto> GenerateReportAsync(ReportType type, ReportParameters parameters, CancellationToken cancellationToken);
    Task<List<ScheduledReportDto>> GetScheduledReportsAsync(CancellationToken cancellationToken);
    Task<ScheduledReportDto> ScheduleReportAsync(ScheduleReportCommand command, CancellationToken cancellationToken);
    Task<bool> CancelScheduledReportAsync(string id, CancellationToken cancellationToken);
}

public interface IConfigurationService
{
    Task<List<ConfigurationDto>> GetConfigurationsAsync(string environment, CancellationToken cancellationToken);
    Task<ConfigurationDto> UpdateConfigurationAsync(string key, string value, string environment, CancellationToken cancellationToken);
}

public interface IUserManagementService
{
    Task<AdminUserDto> CreateAdminUserAsync(CreateUserCommand command, CancellationToken cancellationToken);
    Task<AdminUserDto> UpdateAdminUserAsync(string id, UpdateUserCommand command, CancellationToken cancellationToken);
    Task<dynamic> ResetPasswordAsync(string id, CancellationToken cancellationToken);
}

public interface IKeyManagementService
{
    Task<dynamic> RotateKeysAsync(KeyType keyType, CancellationToken cancellationToken);
}

public interface IMaintenanceService
{
    Task<dynamic> RunDatabaseMaintenanceAsync(CancellationToken cancellationToken);
}

public interface IBackupService
{
    Task<List<BackupDto>> GetBackupHistoryAsync(CancellationToken cancellationToken);
    Task<BackupStatusDto?> GetBackupStatusAsync(string id, CancellationToken cancellationToken);
    Task<dynamic> StartBackupAsync(BackupOptions options, CancellationToken cancellationToken);
    Task<dynamic> StartRestoreAsync(string backupId, RestoreOptions? options, CancellationToken cancellationToken);
}

public interface IFeatureFlagService
{
    Task<List<FeatureFlagDto>> GetAllFlagsAsync(CancellationToken cancellationToken);
    Task<FeatureFlagDto> ToggleFlagAsync(string id, bool enabled, CancellationToken cancellationToken);
}