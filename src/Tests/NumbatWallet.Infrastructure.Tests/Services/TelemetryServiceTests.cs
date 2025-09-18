using System.Text.Json;
using Microsoft.ApplicationInsights;
using Microsoft.ApplicationInsights.Channel;
using Microsoft.ApplicationInsights.DataContracts;
using Microsoft.ApplicationInsights.Extensibility;
using Microsoft.Extensions.Logging;
using Moq;
using NumbatWallet.Application.Interfaces;
using NumbatWallet.Infrastructure.Services;
using NumbatWallet.SharedKernel.Interfaces;
using NumbatWallet.SharedKernel.Enums;
using Xunit;
using FluentAssertions;

namespace NumbatWallet.Infrastructure.Tests.Services;

public class TelemetryServiceTests : IDisposable
{
    private readonly Mock<IRedactionService> _redactionServiceMock;
    private readonly Mock<ICurrentTenantService> _currentTenantServiceMock;
    private readonly Mock<ICurrentUserService> _currentUserServiceMock;
    private readonly Mock<ITelemetryChannel> _telemetryChannelMock;
    private readonly TelemetryClient _telemetryClient;
    private readonly Mock<ILogger<TelemetryService>> _loggerMock;
    private readonly TelemetryService _sut;
    private readonly List<ITelemetry> _capturedTelemetry;

    public TelemetryServiceTests()
    {
        _redactionServiceMock = new Mock<IRedactionService>();
        _currentTenantServiceMock = new Mock<ICurrentTenantService>();
        _currentUserServiceMock = new Mock<ICurrentUserService>();
        _telemetryChannelMock = new Mock<ITelemetryChannel>();
        _loggerMock = new Mock<ILogger<TelemetryService>>();
        _capturedTelemetry = new List<ITelemetry>();

        // Setup telemetry channel to capture telemetry
        _telemetryChannelMock.Setup(x => x.Send(It.IsAny<ITelemetry>()))
            .Callback<ITelemetry>(t => _capturedTelemetry.Add(t));

        var telemetryConfig = new TelemetryConfiguration
        {
            TelemetryChannel = _telemetryChannelMock.Object,
            ConnectionString = "InstrumentationKey=test-key;EndpointSuffix=applicationinsights.azure.net"
        };

        _telemetryClient = new TelemetryClient(telemetryConfig);

        _sut = new TelemetryService(
            _redactionServiceMock.Object,
            _currentTenantServiceMock.Object,
            _currentUserServiceMock.Object,
            _telemetryClient,
            _loggerMock.Object);
    }

    [Fact]
    public async Task LogEventAsync_WithValidEvent_ShouldSendTelemetry()
    {
        // Arrange
        var eventName = "UserLogin";
        var properties = new Dictionary<string, string>
        {
            ["Username"] = "john.doe@example.com",
            ["IpAddress"] = "192.168.1.1"
        };
        var metrics = new Dictionary<string, double>
        {
            ["Duration"] = 250.5
        };

        _currentTenantServiceMock.Setup(x => x.TenantId).Returns("tenant-123");
        _currentUserServiceMock.Setup(x => x.UserId).Returns("user-456");
        _currentUserServiceMock.Setup(x => x.UserEmail).Returns("admin@example.com");

        _redactionServiceMock.Setup(x => x.RedactAsync(
                "john.doe@example.com",
                PiiType.Email,
                RedactionPattern.ShowDomain,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync("***@example.com");

        _redactionServiceMock.Setup(x => x.RedactAsync(
                "192.168.1.1",
                PiiType.IpAddress,
                RedactionPattern.ShowFirstThree,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync("192.168.1.***");

        // Act
        await _sut.LogEventAsync(eventName, properties, metrics);

        // Assert
        _capturedTelemetry.Should().HaveCount(1);
        var eventTelemetry = _capturedTelemetry[0] as EventTelemetry;
        eventTelemetry.Should().NotBeNull();
        eventTelemetry!.Name.Should().Be(eventName);
        eventTelemetry.Properties["Username"].Should().Be("***@example.com");
        eventTelemetry.Properties["IpAddress"].Should().Be("192.168.1.***");
        eventTelemetry.Properties["TenantId"].Should().Be("tenant-123");
        eventTelemetry.Properties["UserId"].Should().Be("user-456");
        eventTelemetry.Metrics["Duration"].Should().Be(250.5);
    }

    [Fact]
    public async Task LogSecurityEventAsync_ShouldCreateSecurityEvent()
    {
        // Arrange
        var eventType = "UnauthorizedAccess";
        var severity = "High";
        var details = new
        {
            Resource = "/api/credentials",
            Action = "DELETE",
            Email = "attacker@example.com"
        };

        _currentTenantServiceMock.Setup(x => x.TenantId).Returns("tenant-123");
        _currentUserServiceMock.Setup(x => x.UserId).Returns("user-456");

        _redactionServiceMock.Setup(x => x.RedactAsync(
                It.IsAny<string>(),
                It.IsAny<PiiType>(),
                It.IsAny<RedactionPattern>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((string value, PiiType _, RedactionPattern __, CancellationToken ___) =>
                value.Contains('@') ? "***@example.com" : value);

        // Act
        await _sut.LogSecurityEventAsync(eventType, severity, details);

        // Assert
        _capturedTelemetry.Should().HaveCount(1);
        var eventTelemetry = _capturedTelemetry[0] as EventTelemetry;
        eventTelemetry.Should().NotBeNull();
        eventTelemetry!.Name.Should().Be($"Security_{eventType}");
        eventTelemetry.Properties["Severity"].Should().Be(severity);
        eventTelemetry.Properties["EventType"].Should().Be(eventType);
        eventTelemetry.Properties.Should().ContainKey("Details");

        var detailsJson = eventTelemetry.Properties["Details"];
        detailsJson.Should().Contain("***@example.com");
        detailsJson.Should().NotContain("attacker@example.com");
    }

    [Fact]
    public async Task LogExceptionAsync_ShouldRedactSensitiveData()
    {
        // Arrange
        var exception = new InvalidOperationException("User john.doe@example.com not found");
        var context = new Dictionary<string, string>
        {
            ["Operation"] = "GetUser",
            ["UserId"] = "user-123"
        };

        _currentTenantServiceMock.Setup(x => x.TenantId).Returns("tenant-123");

        _redactionServiceMock.Setup(x => x.RedactAsync(
                It.IsAny<string>(),
                PiiType.Email,
                RedactionPattern.ShowDomain,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync("***@example.com");

        // Act
        await _sut.LogExceptionAsync(exception, context);

        // Assert
        _capturedTelemetry.Should().HaveCount(1);
        var exceptionTelemetry = _capturedTelemetry[0] as ExceptionTelemetry;
        exceptionTelemetry.Should().NotBeNull();
        exceptionTelemetry!.Exception.Should().BeOfType<InvalidOperationException>();
        exceptionTelemetry.Message.Should().Contain("***@example.com");
        exceptionTelemetry.Message.Should().NotContain("john.doe@example.com");
        exceptionTelemetry.Properties["Operation"].Should().Be("GetUser");
        exceptionTelemetry.Properties["TenantId"].Should().Be("tenant-123");
    }

    [Fact]
    public async Task LogMetricAsync_ShouldTrackMetric()
    {
        // Arrange
        var metricName = "CredentialIssuance";
        var value = 42.0;
        var properties = new Dictionary<string, string>
        {
            ["CredentialType"] = "DriversLicense",
            ["Issuer"] = "DOT"
        };

        _currentTenantServiceMock.Setup(x => x.TenantId).Returns("tenant-123");

        // Act
        await _sut.LogMetricAsync(metricName, value, properties);

        // Assert
        _capturedTelemetry.Should().HaveCount(1);
        var metricTelemetry = _capturedTelemetry[0] as MetricTelemetry;
        metricTelemetry.Should().NotBeNull();
        metricTelemetry!.Name.Should().Be(metricName);
        metricTelemetry.Sum.Should().Be(value);
        metricTelemetry.Properties["CredentialType"].Should().Be("DriversLicense");
        metricTelemetry.Properties["Issuer"].Should().Be("DOT");
        metricTelemetry.Properties["TenantId"].Should().Be("tenant-123");
    }

    [Fact]
    public async Task LogDependencyAsync_ShouldTrackDependency()
    {
        // Arrange
        var dependencyType = "HTTP";
        var dependencyName = "ServiceWA API";
        var data = "GET /api/users/john.doe@example.com";
        var startTime = DateTimeOffset.UtcNow;
        var duration = TimeSpan.FromMilliseconds(150);
        var success = true;

        _currentTenantServiceMock.Setup(x => x.TenantId).Returns("tenant-123");

        _redactionServiceMock.Setup(x => x.RedactAsync(
                "john.doe@example.com",
                PiiType.Email,
                RedactionPattern.ShowDomain,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync("***@example.com");

        // Act
        await _sut.LogDependencyAsync(
            dependencyType,
            dependencyName,
            data,
            startTime,
            duration,
            success);

        // Assert
        _capturedTelemetry.Should().HaveCount(1);
        var dependencyTelemetry = _capturedTelemetry[0] as DependencyTelemetry;
        dependencyTelemetry.Should().NotBeNull();
        dependencyTelemetry!.Type.Should().Be(dependencyType);
        dependencyTelemetry.Name.Should().Be(dependencyName);
        dependencyTelemetry.Data.Should().Be("GET /api/users/***@example.com");
        dependencyTelemetry.Timestamp.Should().Be(startTime);
        dependencyTelemetry.Duration.Should().Be(duration);
        dependencyTelemetry.Success.Should().Be(success);
        dependencyTelemetry.Properties["TenantId"].Should().Be("tenant-123");
    }

    [Fact]
    public async Task LogPageViewAsync_ShouldTrackPageView()
    {
        // Arrange
        var pageName = "UserProfile";
        var url = "/users/profile?email=john.doe@example.com";
        var duration = TimeSpan.FromSeconds(2.5);
        var properties = new Dictionary<string, string>
        {
            ["Section"] = "Admin",
            ["UserEmail"] = "john.doe@example.com"
        };

        _currentTenantServiceMock.Setup(x => x.TenantId).Returns("tenant-123");
        _currentUserServiceMock.Setup(x => x.UserId).Returns("user-456");

        _redactionServiceMock.Setup(x => x.RedactAsync(
                It.IsAny<string>(),
                PiiType.Email,
                RedactionPattern.ShowDomain,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((string value, PiiType _, RedactionPattern __, CancellationToken ___) =>
                value.Replace("john.doe", "***"));

        // Act
        await _sut.LogPageViewAsync(pageName, url, duration, properties);

        // Assert
        _capturedTelemetry.Should().HaveCount(1);
        var pageViewTelemetry = _capturedTelemetry[0] as PageViewTelemetry;
        pageViewTelemetry.Should().NotBeNull();
        pageViewTelemetry!.Name.Should().Be(pageName);
        pageViewTelemetry.Url.ToString().Should().Contain("***@example.com");
        pageViewTelemetry.Url.ToString().Should().NotContain("john.doe");
        pageViewTelemetry.Duration.Should().Be(duration);
        pageViewTelemetry.Properties["UserEmail"].Should().Be("***@example.com");
        pageViewTelemetry.Properties["TenantId"].Should().Be("tenant-123");
    }

    [Fact]
    public async Task LogRequestAsync_ShouldTrackRequest()
    {
        // Arrange
        var requestName = "POST /api/credentials";
        var startTime = DateTimeOffset.UtcNow;
        var duration = TimeSpan.FromMilliseconds(345);
        var responseCode = "200";
        var success = true;
        var properties = new Dictionary<string, string>
        {
            ["RequestBody"] = "{\"email\":\"john.doe@example.com\"}",
            ["ClientIp"] = "192.168.1.100"
        };

        _currentTenantServiceMock.Setup(x => x.TenantId).Returns("tenant-123");

        _redactionServiceMock.Setup(x => x.RedactAsync(
                It.IsAny<string>(),
                It.IsAny<PiiType>(),
                It.IsAny<RedactionPattern>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((string value, PiiType piiType, RedactionPattern pattern, CancellationToken _) =>
                {
                    if (value.Contains('@'))
                    {
                        return value.Replace("john.doe", "***");
                    }

                    if (piiType == PiiType.IpAddress)
                    {
                        return "192.168.1.***";
                    }

                    return value;
                });

        // Act
        await _sut.LogRequestAsync(
            requestName,
            startTime,
            duration,
            responseCode,
            success,
            properties);

        // Assert
        _capturedTelemetry.Should().HaveCount(1);
        var requestTelemetry = _capturedTelemetry[0] as RequestTelemetry;
        requestTelemetry.Should().NotBeNull();
        requestTelemetry!.Name.Should().Be(requestName);
        requestTelemetry.Timestamp.Should().Be(startTime);
        requestTelemetry.Duration.Should().Be(duration);
        requestTelemetry.ResponseCode.Should().Be(responseCode);
        requestTelemetry.Success.Should().Be(success);
        requestTelemetry.Properties["RequestBody"].Should().Contain("***@example.com");
        requestTelemetry.Properties["ClientIp"].Should().Be("192.168.1.***");
    }

    [Fact]
    public async Task LogTraceAsync_ShouldWriteTrace()
    {
        // Arrange
        var message = "Processing credential for john.doe@example.com";
        var severityLevel = TelemetrySeverityLevel.Information;
        var properties = new Dictionary<string, string>
        {
            ["CredentialType"] = "PassportCredential"
        };

        _currentTenantServiceMock.Setup(x => x.TenantId).Returns("tenant-123");

        _redactionServiceMock.Setup(x => x.RedactAsync(
                "john.doe@example.com",
                PiiType.Email,
                RedactionPattern.ShowDomain,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync("***@example.com");

        // Act
        await _sut.LogTraceAsync(message, severityLevel, properties);

        // Assert
        _capturedTelemetry.Should().HaveCount(1);
        var traceTelemetry = _capturedTelemetry[0] as TraceTelemetry;
        traceTelemetry.Should().NotBeNull();
        traceTelemetry!.Message.Should().Be("Processing credential for ***@example.com");
        traceTelemetry.SeverityLevel.Should().Be(SeverityLevel.Information);
        traceTelemetry.Properties["CredentialType"].Should().Be("PassportCredential");
        traceTelemetry.Properties["TenantId"].Should().Be("tenant-123");
    }

    [Fact]
    public async Task LogAvailabilityAsync_ShouldTrackAvailability()
    {
        // Arrange
        var testName = "DatabaseHealth";
        var startTime = DateTimeOffset.UtcNow;
        var duration = TimeSpan.FromMilliseconds(50);
        var runLocation = "Australia East";
        var success = true;
        var message = "Database is healthy";

        _currentTenantServiceMock.Setup(x => x.TenantId).Returns("tenant-123");

        // Act
        await _sut.LogAvailabilityAsync(
            testName,
            startTime,
            duration,
            runLocation,
            success,
            message);

        // Assert
        _capturedTelemetry.Should().HaveCount(1);
        var availabilityTelemetry = _capturedTelemetry[0] as AvailabilityTelemetry;
        availabilityTelemetry.Should().NotBeNull();
        availabilityTelemetry!.Name.Should().Be(testName);
        availabilityTelemetry.Timestamp.Should().Be(startTime);
        availabilityTelemetry.Duration.Should().Be(duration);
        availabilityTelemetry.RunLocation.Should().Be(runLocation);
        availabilityTelemetry.Success.Should().Be(success);
        availabilityTelemetry.Message.Should().Be(message);
        availabilityTelemetry.Properties["TenantId"].Should().Be("tenant-123");
    }

    [Fact]
    public async Task RedactPropertiesAsync_ShouldDetectAndRedactPII()
    {
        // Arrange
        var properties = new Dictionary<string, string>
        {
            ["Email"] = "john.doe@example.com",
            ["Phone"] = "+61412345678",
            ["Name"] = "John Doe",
            ["IpAddress"] = "192.168.1.1",
            ["SafeData"] = "This is safe",
            ["AccountNumber"] = "1234567890",
            ["DateOfBirth"] = "1990-01-15"
        };

        _redactionServiceMock.Setup(x => x.RedactAsync(
                "john.doe@example.com",
                PiiType.Email,
                RedactionPattern.ShowDomain,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync("***@example.com");

        _redactionServiceMock.Setup(x => x.RedactAsync(
                "+61412345678",
                PiiType.Phone,
                RedactionPattern.ShowLastFour,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync("+614****5678");

        _redactionServiceMock.Setup(x => x.RedactAsync(
                "John Doe",
                PiiType.Name,
                RedactionPattern.ShowFirstThree,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync("Joh***");

        _redactionServiceMock.Setup(x => x.RedactAsync(
                "192.168.1.1",
                PiiType.IpAddress,
                RedactionPattern.ShowFirstThree,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync("192.168.1.***");

        _redactionServiceMock.Setup(x => x.RedactAsync(
                "1234567890",
                PiiType.AccountNumber,
                RedactionPattern.ShowLastFour,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync("******7890");

        _redactionServiceMock.Setup(x => x.RedactAsync(
                "1990-01-15",
                PiiType.DateOfBirth,
                RedactionPattern.Full,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync("***");

        // Act
        await _sut.LogEventAsync("TestEvent", properties);

        // Assert
        _capturedTelemetry.Should().HaveCount(1);
        var eventTelemetry = _capturedTelemetry[0] as EventTelemetry;
        eventTelemetry.Should().NotBeNull();
        eventTelemetry!.Properties["Email"].Should().Be("***@example.com");
        eventTelemetry.Properties["Phone"].Should().Be("+614****5678");
        eventTelemetry.Properties["Name"].Should().Be("Joh***");
        eventTelemetry.Properties["IpAddress"].Should().Be("192.168.1.***");
        eventTelemetry.Properties["SafeData"].Should().Be("This is safe");
        eventTelemetry.Properties["AccountNumber"].Should().Be("******7890");
        eventTelemetry.Properties["DateOfBirth"].Should().Be("***");
    }

    [Fact]
    public async Task LogEventAsync_WithNullProperties_ShouldHandleGracefully()
    {
        // Arrange
        var eventName = "SimpleEvent";
        Dictionary<string, string>? properties = null;
        Dictionary<string, double>? metrics = null;

        _currentTenantServiceMock.Setup(x => x.TenantId).Returns("tenant-123");

        // Act
        await _sut.LogEventAsync(eventName, properties, metrics);

        // Assert
        _capturedTelemetry.Should().HaveCount(1);
        var eventTelemetry = _capturedTelemetry[0] as EventTelemetry;
        eventTelemetry.Should().NotBeNull();
        eventTelemetry!.Name.Should().Be(eventName);
        eventTelemetry.Properties["TenantId"].Should().Be("tenant-123");
        eventTelemetry.Metrics.Should().BeEmpty();
    }

    public void Dispose()
    {
        _telemetryChannelMock.Object?.Flush();
        (_telemetryChannelMock.Object as IDisposable)?.Dispose();
    }
}