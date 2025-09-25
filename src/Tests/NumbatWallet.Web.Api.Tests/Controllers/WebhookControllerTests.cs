using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using NumbatWallet.Web.Api.Controllers;
using NumbatWallet.Web.Api.Security;
using NumbatWallet.Web.Api.Webhooks;

namespace NumbatWallet.Web.Api.Tests.Controllers;

public class WebhookControllerTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;
    private readonly Mock<IWebhookService> _mockWebhookService;
    private readonly Mock<ISecurityAuditService> _mockAuditService;

    public WebhookControllerTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
        _mockWebhookService = new Mock<IWebhookService>();
        _mockAuditService = new Mock<ISecurityAuditService>();
    }

    private HttpClient CreateClient()
    {
        return _factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureServices(services =>
            {
                // Remove existing registrations
                var webhookDescriptor = services.SingleOrDefault(d => d.ServiceType == typeof(IWebhookService));
                if (webhookDescriptor != null)
                {
                    services.Remove(webhookDescriptor);
                }

                var auditDescriptor = services.SingleOrDefault(d => d.ServiceType == typeof(ISecurityAuditService));
                if (auditDescriptor != null)
                {
                    services.Remove(auditDescriptor);
                }

                // Add mocks
                services.AddSingleton(_mockWebhookService.Object);
                services.AddSingleton(_mockAuditService.Object);
            });
        }).CreateClient();
    }

    [Fact]
    public async Task Subscribe_WithValidRequest_ReturnsCreated()
    {
        // Arrange
        var client = CreateClient();
        client.DefaultRequestHeaders.Add("Authorization", "Bearer test-token");

        var request = new WebhookSubscriptionRequestDto
        {
            Url = "https://example.com/webhook",
            Events = new List<WebhookEventType>
            {
                WebhookEventType.CredentialIssued,
                WebhookEventType.CredentialRevoked
            },
            Headers = new Dictionary<string, string> { ["X-API-Key"] = "test-key" },
            MaxRetries = 3,
            TimeoutSeconds = 30
        };

        var expectedId = Guid.NewGuid();
        var expectedSubscription = new WebhookSubscription
        {
            Id = expectedId,
            Url = request.Url,
            Secret = "whsec_123456",
            Events = request.Events,
            IsActive = true,
            Headers = request.Headers,
            MaxRetries = request.MaxRetries ?? 3,
            TimeoutSeconds = request.TimeoutSeconds ?? 30,
            CreatedAt = DateTime.UtcNow
        };

        _mockWebhookService
            .Setup(x => x.RegisterWebhookAsync(It.IsAny<WebhookSubscription>(), default))
            .ReturnsAsync(expectedId);

        _mockWebhookService
            .Setup(x => x.GetWebhookAsync(expectedId, default))
            .ReturnsAsync(expectedSubscription);

        // Act
        var response = await client.PostAsJsonAsync("/api/v1/webhook/subscribe", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var result = await response.Content.ReadFromJsonAsync<WebhookSubscriptionResponseDto>();
        result.Should().NotBeNull();
        result!.Id.Should().Be(expectedId);
        result.Url.Should().Be(request.Url);
        result.Secret.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task GetSubscription_WithExistingId_ReturnsSubscription()
    {
        // Arrange
        var client = CreateClient();
        client.DefaultRequestHeaders.Add("Authorization", "Bearer test-token");

        var subscriptionId = Guid.NewGuid();
        var expectedSubscription = new WebhookSubscription
        {
            Id = subscriptionId,
            Url = "https://example.com/webhook",
            Secret = "whsec_123456",
            Events = new List<WebhookEventType> { WebhookEventType.WalletCreated },
            IsActive = true,
            CreatedAt = DateTime.UtcNow.AddDays(-7),
            LastDeliveryAt = DateTime.UtcNow.AddHours(-2),
            ConsecutiveFailures = 0
        };

        _mockWebhookService
            .Setup(x => x.GetWebhookAsync(subscriptionId, default))
            .ReturnsAsync(expectedSubscription);

        // Act
        var response = await client.GetAsync($"/api/v1/webhook/{subscriptionId}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<WebhookSubscriptionResponseDto>();
        result.Should().NotBeNull();
        result!.Id.Should().Be(subscriptionId);
        result.Url.Should().Be(expectedSubscription.Url);
    }

    [Fact]
    public async Task GetSubscription_WithNonExistentId_ReturnsNotFound()
    {
        // Arrange
        var client = CreateClient();
        client.DefaultRequestHeaders.Add("Authorization", "Bearer test-token");

        var subscriptionId = Guid.NewGuid();

        _mockWebhookService
            .Setup(x => x.GetWebhookAsync(subscriptionId, default))
            .ReturnsAsync((WebhookSubscription?)null);

        // Act
        var response = await client.GetAsync($"/api/v1/webhook/{subscriptionId}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        var content = await response.Content.ReadAsStringAsync();
        content.Should().Contain($"Webhook subscription {subscriptionId} not found");
    }

    [Fact]
    public async Task GetSubscriptions_ReturnsAllActiveSubscriptions()
    {
        // Arrange
        var client = CreateClient();
        client.DefaultRequestHeaders.Add("Authorization", "Bearer test-token");

        var subscriptions = new List<WebhookSubscription>
        {
            new WebhookSubscription
            {
                Id = Guid.NewGuid(),
                Url = "https://example1.com/webhook",
                Secret = "secret1",
                Events = new List<WebhookEventType> { WebhookEventType.CredentialIssued },
                IsActive = true
            },
            new WebhookSubscription
            {
                Id = Guid.NewGuid(),
                Url = "https://example2.com/webhook",
                Secret = "secret2",
                Events = new List<WebhookEventType> { WebhookEventType.WalletCreated },
                IsActive = true
            }
        };

        _mockWebhookService
            .Setup(x => x.GetActiveWebhooksAsync(default))
            .ReturnsAsync(subscriptions);

        // Act
        var response = await client.GetAsync("/api/v1/webhook");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<IEnumerable<WebhookSubscriptionResponseDto>>();
        result.Should().NotBeNull();
        result.Should().HaveCount(2);
    }

    [Fact]
    public async Task Unsubscribe_WithExistingId_ReturnsNoContent()
    {
        // Arrange
        var client = CreateClient();
        client.DefaultRequestHeaders.Add("Authorization", "Bearer test-token");

        var subscriptionId = Guid.NewGuid();

        _mockWebhookService
            .Setup(x => x.UnregisterWebhookAsync(subscriptionId, default))
            .ReturnsAsync(true);

        // Act
        var response = await client.DeleteAsync($"/api/v1/webhook/{subscriptionId}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NoContent);

        // Verify audit was logged
        _mockAuditService.Verify(x => x.LogSecurityEventAsync(
            It.IsAny<HttpContext>(),
            SecurityEventType.ConfigurationChange,
            It.Is<string>(s => s.Contains("Webhook subscription removed")),
            It.IsAny<bool>()),
            Times.Once);
    }

    [Fact]
    public async Task Unsubscribe_WithNonExistentId_ReturnsNotFound()
    {
        // Arrange
        var client = CreateClient();
        client.DefaultRequestHeaders.Add("Authorization", "Bearer test-token");

        var subscriptionId = Guid.NewGuid();

        _mockWebhookService
            .Setup(x => x.UnregisterWebhookAsync(subscriptionId, default))
            .ReturnsAsync(false);

        // Act
        var response = await client.DeleteAsync($"/api/v1/webhook/{subscriptionId}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task TestWebhook_WithExistingId_ReturnsTestResult()
    {
        // Arrange
        var client = CreateClient();
        client.DefaultRequestHeaders.Add("Authorization", "Bearer test-token");

        var subscriptionId = Guid.NewGuid();
        var subscription = new WebhookSubscription
        {
            Id = subscriptionId,
            Url = "https://example.com/webhook",
            Secret = "secret",
            Events = new List<WebhookEventType> { WebhookEventType.WalletCreated },
            IsActive = true
        };

        var deliveryResult = new WebhookDeliveryResult
        {
            Success = true,
            StatusCode = 200,
            Duration = TimeSpan.FromMilliseconds(250),
            DeliveredAt = DateTime.UtcNow
        };

        _mockWebhookService
            .Setup(x => x.GetWebhookAsync(subscriptionId, default))
            .ReturnsAsync(subscription);

        _mockWebhookService
            .Setup(x => x.SendWebhookAsync(WebhookEventType.WalletCreated, It.IsAny<object>(), default))
            .ReturnsAsync(deliveryResult);

        // Act
        var response = await client.PostAsync($"/api/v1/webhook/{subscriptionId}/test", null);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<WebhookTestResponseDto>();
        result.Should().NotBeNull();
        result!.Success.Should().BeTrue();
        result.StatusCode.Should().Be(200);
    }

    [Fact]
    public async Task TestWebhook_WithFailedDelivery_ReturnsFailureDetails()
    {
        // Arrange
        var client = CreateClient();
        client.DefaultRequestHeaders.Add("Authorization", "Bearer test-token");

        var subscriptionId = Guid.NewGuid();
        var subscription = new WebhookSubscription
        {
            Id = subscriptionId,
            Url = "https://example.com/webhook",
            Secret = "secret",
            Events = new List<WebhookEventType> { WebhookEventType.WalletCreated },
            IsActive = true
        };

        var deliveryResult = new WebhookDeliveryResult
        {
            Success = false,
            StatusCode = 500,
            ErrorMessage = "Internal server error",
            Duration = TimeSpan.FromSeconds(5),
            DeliveredAt = DateTime.UtcNow
        };

        _mockWebhookService
            .Setup(x => x.GetWebhookAsync(subscriptionId, default))
            .ReturnsAsync(subscription);

        _mockWebhookService
            .Setup(x => x.SendWebhookAsync(WebhookEventType.WalletCreated, It.IsAny<object>(), default))
            .ReturnsAsync(deliveryResult);

        // Act
        var response = await client.PostAsync($"/api/v1/webhook/{subscriptionId}/test", null);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<WebhookTestResponseDto>();
        result.Should().NotBeNull();
        result!.Success.Should().BeFalse();
        result.StatusCode.Should().Be(500);
        result.ErrorMessage.Should().Be("Internal server error");
    }

    [Fact]
    public async Task ValidateSignature_WithValidSignature_ReturnsValid()
    {
        // Arrange
        var client = CreateClient();

        var request = new ValidateWebhookRequestDto
        {
            Payload = "{\"event\":\"test\"}",
            Signature = "sha256=abcdef123456",
            Secret = "whsec_secret"
        };

        _mockWebhookService
            .Setup(x => x.ValidateWebhookSignatureAsync(request.Payload, request.Signature, request.Secret))
            .ReturnsAsync(true);

        // Act
        var response = await client.PostAsJsonAsync("/api/v1/webhook/validate", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<WebhookValidationResponseDto>();
        result.Should().NotBeNull();
        result!.IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task ValidateSignature_WithInvalidSignature_ReturnsInvalid()
    {
        // Arrange
        var client = CreateClient();

        var request = new ValidateWebhookRequestDto
        {
            Payload = "{\"event\":\"test\"}",
            Signature = "sha256=invalid",
            Secret = "whsec_secret"
        };

        _mockWebhookService
            .Setup(x => x.ValidateWebhookSignatureAsync(request.Payload, request.Signature, request.Secret))
            .ReturnsAsync(false);

        // Act
        var response = await client.PostAsJsonAsync("/api/v1/webhook/validate", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<WebhookValidationResponseDto>();
        result.Should().NotBeNull();
        result!.IsValid.Should().BeFalse();
    }

    [Fact]
    public async Task Subscribe_LogsSecurityAudit()
    {
        // Arrange
        var client = CreateClient();
        client.DefaultRequestHeaders.Add("Authorization", "Bearer test-token");

        var request = new WebhookSubscriptionRequestDto
        {
            Url = "https://example.com/webhook",
            Events = new List<WebhookEventType> { WebhookEventType.CredentialIssued }
        };

        var expectedId = Guid.NewGuid();
        var expectedSubscription = new WebhookSubscription
        {
            Id = expectedId,
            Url = request.Url,
            Secret = "secret",
            Events = request.Events,
            IsActive = true
        };

        _mockWebhookService
            .Setup(x => x.RegisterWebhookAsync(It.IsAny<WebhookSubscription>(), default))
            .ReturnsAsync(expectedId);

        _mockWebhookService
            .Setup(x => x.GetWebhookAsync(expectedId, default))
            .ReturnsAsync(expectedSubscription);

        // Act
        await client.PostAsJsonAsync("/api/v1/webhook/subscribe", request);

        // Assert
        _mockAuditService.Verify(x => x.LogSecurityEventAsync(
            It.IsAny<HttpContext>(),
            SecurityEventType.ConfigurationChange,
            It.Is<string>(s => s.Contains("Webhook subscription created")),
            It.IsAny<bool>()),
            Times.Once);
    }
}