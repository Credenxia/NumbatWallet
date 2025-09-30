using NumbatWallet.Web.Api.Security;
using NumbatWallet.Web.Api.Webhooks;

namespace NumbatWallet.Web.Api.Controllers;

[ApiController]
[Asp.Versioning.ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/[controller]")]
[Microsoft.AspNetCore.Authorization.Authorize(Roles = "Admin")]
[Produces("application/json")]
public class WebhookController : ControllerBase
{
    private readonly IWebhookService _webhookService;
    private readonly ISecurityAuditService _auditService;
    private readonly ILogger<WebhookController> _logger;

    public WebhookController(
        IWebhookService webhookService,
        ISecurityAuditService auditService,
        ILogger<WebhookController> logger)
    {
        _webhookService = webhookService;
        _auditService = auditService;
        _logger = logger;
    }

    /// <summary>
    /// Register a new webhook subscription
    /// </summary>
    [HttpPost("subscribe")]
    [ProducesResponseType(typeof(WebhookSubscriptionResponseDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Subscribe([FromBody] WebhookSubscriptionRequestDto request)
    {
        _logger.LogInformation("Registering webhook for URL {Url}", request.Url);

        await _auditService.LogSecurityEventAsync(
            HttpContext,
            SecurityEventType.ConfigurationChange,
            $"Webhook subscription created for {request.Url}");

        var subscription = new WebhookSubscription
        {
            Url = request.Url,
            Events = request.Events,
            Headers = request.Headers ?? new Dictionary<string, string>(),
            MaxRetries = request.MaxRetries ?? 3,
            TimeoutSeconds = request.TimeoutSeconds ?? 30
        };

        var subscriptionId = await _webhookService.RegisterWebhookAsync(subscription);

        var registeredSubscription = await _webhookService.GetWebhookAsync(subscriptionId);

        return CreatedAtAction(
            nameof(GetSubscription),
            new { id = subscriptionId },
            new WebhookSubscriptionResponseDto
            {
                Id = subscriptionId,
                Url = registeredSubscription!.Url,
                Secret = registeredSubscription.Secret,
                Events = registeredSubscription.Events,
                IsActive = registeredSubscription.IsActive
            });
    }

    /// <summary>
    /// Get a webhook subscription by ID
    /// </summary>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(WebhookSubscriptionResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetSubscription(Guid id)
    {
        var subscription = await _webhookService.GetWebhookAsync(id);

        if (subscription == null)
        {
            return NotFound($"Webhook subscription {id} not found");
        }

        return Ok(new WebhookSubscriptionResponseDto
        {
            Id = subscription.Id,
            Url = subscription.Url,
            Secret = subscription.Secret,
            Events = subscription.Events,
            IsActive = subscription.IsActive,
            Headers = subscription.Headers,
            MaxRetries = subscription.MaxRetries,
            TimeoutSeconds = subscription.TimeoutSeconds,
            CreatedAt = subscription.CreatedAt,
            LastDeliveryAt = subscription.LastDeliveryAt,
            ConsecutiveFailures = subscription.ConsecutiveFailures
        });
    }

    /// <summary>
    /// Get all active webhook subscriptions
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<WebhookSubscriptionResponseDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetSubscriptions()
    {
        var subscriptions = await _webhookService.GetActiveWebhooksAsync();

        var response = subscriptions.Select(s => new WebhookSubscriptionResponseDto
        {
            Id = s.Id,
            Url = s.Url,
            Secret = s.Secret,
            Events = s.Events,
            IsActive = s.IsActive,
            Headers = s.Headers,
            MaxRetries = s.MaxRetries,
            TimeoutSeconds = s.TimeoutSeconds,
            CreatedAt = s.CreatedAt,
            LastDeliveryAt = s.LastDeliveryAt,
            ConsecutiveFailures = s.ConsecutiveFailures
        });

        return Ok(response);
    }

    /// <summary>
    /// Unsubscribe from webhook notifications
    /// </summary>
    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Unsubscribe(Guid id)
    {
        _logger.LogInformation("Unregistering webhook {WebhookId}", id);

        await _auditService.LogSecurityEventAsync(
            HttpContext,
            SecurityEventType.ConfigurationChange,
            $"Webhook subscription removed: {id}");

        var result = await _webhookService.UnregisterWebhookAsync(id);

        if (!result)
        {
            return NotFound($"Webhook subscription {id} not found");
        }

        return NoContent();
    }

    /// <summary>
    /// Test a webhook subscription
    /// </summary>
    [HttpPost("{id:guid}/test")]
    [ProducesResponseType(typeof(WebhookTestResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> TestWebhook(Guid id)
    {
        var subscription = await _webhookService.GetWebhookAsync(id);

        if (subscription == null)
        {
            return NotFound($"Webhook subscription {id} not found");
        }

        _logger.LogInformation("Testing webhook {WebhookId}", id);

        var testData = new
        {
            @event = "test",
            message = "Test webhook delivery",
            timestamp = DateTime.UtcNow,
            subscriptionId = id
        };

        var result = await _webhookService.DeliverWebhookAsync(subscription, testData);

        return Ok(new WebhookTestResponseDto
        {
            Success = result.Success,
            StatusCode = result.StatusCode,
            Duration = result.Duration,
            ErrorMessage = result.ErrorMessage,
            DeliveredAt = result.DeliveredAt
        });
    }

    /// <summary>
    /// Validate a webhook signature
    /// </summary>
    [HttpPost("validate")]
    [Microsoft.AspNetCore.Authorization.AllowAnonymous]
    [ProducesResponseType(typeof(WebhookValidationResponseDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> ValidateSignature([FromBody] ValidateWebhookRequestDto request)
    {
        var isValid = await _webhookService.ValidateWebhookSignatureAsync(
            request.Payload,
            request.Signature,
            request.Secret);

        return Ok(new WebhookValidationResponseDto
        {
            IsValid = isValid,
            Timestamp = DateTime.UtcNow
        });
    }


    /// <summary>
    /// Validate webhook signature
    /// </summary>
    [HttpPost("validate-signature")]
    [ProducesResponseType(typeof(SignatureValidationResultDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> ValidateSignature([FromBody] ValidateSignatureRequestDto request)
    {
        var subscription = await _webhookService.GetWebhookAsync(request.SubscriptionId);

        if (subscription == null)
        {
            return Ok(new SignatureValidationResultDto
            {
                IsValid = false,
                Error = "Subscription not found"
            });
        }

        var isValid = _webhookService.ValidateWebhookSignature(
            request.Payload,
            request.Signature,
            subscription.Secret);

        return Ok(new SignatureValidationResultDto
        {
            IsValid = isValid,
            Error = isValid ? null : "Invalid signature"
        });
    }
}

// DTOs for webhook management
public class WebhookTestResultDto
{
    public bool Success { get; set; }
    public int? StatusCode { get; set; }
    public string? ResponseBody { get; set; }
    public int? ResponseTime { get; set; }
    public string? Error { get; set; }
}

public class WebhookTestResponseDto
{
    public bool Success { get; set; }
    public int StatusCode { get; set; }
    public TimeSpan Duration { get; set; }
    public string? ErrorMessage { get; set; }
    public DateTime DeliveredAt { get; set; }
}

public class SignatureValidationResultDto
{
    public bool IsValid { get; set; }
    public string? Error { get; set; }
}

public class ValidateSignatureRequestDto
{
    public Guid SubscriptionId { get; set; }
    public string Payload { get; set; } = string.Empty;
    public string Signature { get; set; } = string.Empty;
}

public class WebhookSubscriptionRequestDto
{
    public required string Url { get; set; }
    public required List<WebhookEventType> Events { get; set; }
    public Dictionary<string, string>? Headers { get; set; }
    public int? MaxRetries { get; set; }
    public int? TimeoutSeconds { get; set; }
}

public class WebhookSubscriptionResponseDto
{
    public Guid Id { get; set; }
    public required string Url { get; set; }
    public required string Secret { get; set; }
    public required List<WebhookEventType> Events { get; set; }
    public bool IsActive { get; set; }
    public Dictionary<string, string> Headers { get; set; } = new();
    public int MaxRetries { get; set; }
    public int TimeoutSeconds { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? LastDeliveryAt { get; set; }
    public int ConsecutiveFailures { get; set; }
}

public class ValidateWebhookRequestDto
{
    public required string Payload { get; set; }
    public required string Signature { get; set; }
    public required string Secret { get; set; }
}

public class WebhookValidationResponseDto
{
    public bool IsValid { get; set; }
    public DateTime Timestamp { get; set; }
}