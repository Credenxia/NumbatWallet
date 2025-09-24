using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using NumbatWallet.Application.Interfaces;
using Polly;
using Polly.Extensions.Http;

namespace NumbatWallet.Web.Api.Webhooks;

/// <summary>
/// Webhook event types
/// </summary>
public enum WebhookEventType
{
    CredentialIssued,
    CredentialRevoked,
    CredentialExpired,
    CredentialVerified,
    IssuanceRequested,
    IssuanceApproved,
    IssuanceRejected,
    IssuanceCompleted,
    WalletCreated,
    WalletUpdated,
    WalletDeleted
}

/// <summary>
/// Webhook payload
/// </summary>
public class WebhookPayload
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public WebhookEventType EventType { get; set; }
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    public string Source { get; set; } = "NumbatWallet";
    public object Data { get; set; } = new { };
    public Dictionary<string, string> Metadata { get; set; } = new();
}

/// <summary>
/// Webhook subscription
/// </summary>
public class WebhookSubscription
{
    public Guid Id { get; set; }
    public string Url { get; set; } = string.Empty;
    public string Secret { get; set; } = string.Empty;
    public List<WebhookEventType> Events { get; set; } = new();
    public bool IsActive { get; set; } = true;
    public Dictionary<string, string> Headers { get; set; } = new();
    public int MaxRetries { get; set; } = 3;
    public int TimeoutSeconds { get; set; } = 30;
    public DateTime CreatedAt { get; set; }
    public DateTime? LastDeliveryAt { get; set; }
    public int ConsecutiveFailures { get; set; }
}

/// <summary>
/// Webhook delivery result
/// </summary>
public class WebhookDeliveryResult
{
    public bool Success { get; set; }
    public int StatusCode { get; set; }
    public string? ErrorMessage { get; set; }
    public string? Error { get; set; }
    public string? ResponseBody { get; set; }
    public int? ResponseTime { get; set; }
    public TimeSpan Duration { get; set; }
    public DateTime DeliveredAt { get; set; }
    public int AttemptNumber { get; set; }
}

/// <summary>
/// Webhook service interface
/// </summary>
public interface IWebhookService
{
    Task<Guid> RegisterWebhookAsync(WebhookSubscription subscription, CancellationToken cancellationToken = default);
    Task<bool> UnregisterWebhookAsync(Guid subscriptionId, CancellationToken cancellationToken = default);
    Task<WebhookSubscription?> GetWebhookAsync(Guid subscriptionId, CancellationToken cancellationToken = default);
    Task<IEnumerable<WebhookSubscription>> GetActiveWebhooksAsync(CancellationToken cancellationToken = default);
    Task<IEnumerable<WebhookSubscription>> GetAllWebhooksAsync(CancellationToken cancellationToken = default);
    Task<WebhookDeliveryResult> SendWebhookAsync(WebhookEventType eventType, object data, CancellationToken cancellationToken = default);
    Task<WebhookDeliveryResult> DeliverWebhookAsync(WebhookSubscription subscription, object payload, CancellationToken cancellationToken = default);
    Task<bool> ValidateWebhookSignatureAsync(string payload, string signature, string secret);
    bool ValidateWebhookSignature(string payload, string signature, string secret);
}

/// <summary>
/// Webhook service implementation
/// </summary>
public class WebhookService : IWebhookService
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ICacheService _cacheService;
    private readonly ILogger<WebhookService> _logger;
    private readonly Polly.Retry.AsyncRetryPolicy<HttpResponseMessage> _retryPolicy;
    private readonly JsonSerializerOptions _jsonOptions;

    public WebhookService(
        IHttpClientFactory httpClientFactory,
        ICacheService cacheService,
        ILogger<WebhookService> logger)
    {
        _httpClientFactory = httpClientFactory;
        _cacheService = cacheService;
        _logger = logger;

        // Configure retry policy with exponential backoff
        _retryPolicy = HttpPolicyExtensions
            .HandleTransientHttpError()
            .OrResult(msg => !msg.IsSuccessStatusCode)
            .WaitAndRetryAsync(
                3,
                retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)),
                onRetry: (outcome, timespan, retryCount, context) =>
                {
                    _logger.LogWarning("Webhook delivery retry {RetryCount} after {Delay}ms",
                        retryCount, timespan.TotalMilliseconds);
                });

        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = false
        };
    }

    public async Task<Guid> RegisterWebhookAsync(WebhookSubscription subscription, CancellationToken cancellationToken = default)
    {
        subscription.Id = Guid.NewGuid();
        subscription.CreatedAt = DateTime.UtcNow;
        subscription.Secret = GenerateWebhookSecret();

        // Store subscription in cache (in production, use persistent storage)
        await _cacheService.SetAsync(
            $"webhook:{subscription.Id}",
            subscription,
            TimeSpan.FromDays(365),
            cancellationToken);

        // Add to active webhooks index
        var activeWebhooks = await GetActiveWebhooksListAsync(cancellationToken);
        activeWebhooks.Add(subscription.Id);
        await _cacheService.SetAsync(
            "webhooks:active",
            activeWebhooks,
            TimeSpan.FromDays(365),
            cancellationToken);

        _logger.LogInformation("Registered webhook {WebhookId} for URL {Url}",
            subscription.Id, subscription.Url);

        return subscription.Id;
    }

    public async Task<bool> UnregisterWebhookAsync(Guid subscriptionId, CancellationToken cancellationToken = default)
    {
        var subscription = await GetWebhookAsync(subscriptionId, cancellationToken);
        if (subscription == null)
        {
            return false;
        }

        // Remove from cache
        await _cacheService.RemoveAsync($"webhook:{subscriptionId}", cancellationToken);

        // Remove from active webhooks index
        var activeWebhooks = await GetActiveWebhooksListAsync(cancellationToken);
        activeWebhooks.Remove(subscriptionId);
        await _cacheService.SetAsync(
            "webhooks:active",
            activeWebhooks,
            TimeSpan.FromDays(365),
            cancellationToken);

        _logger.LogInformation("Unregistered webhook {WebhookId}", subscriptionId);

        return true;
    }

    public async Task<WebhookSubscription?> GetWebhookAsync(Guid subscriptionId, CancellationToken cancellationToken = default)
    {
        return await _cacheService.GetAsync<WebhookSubscription>(
            $"webhook:{subscriptionId}",
            cancellationToken);
    }

    public async Task<IEnumerable<WebhookSubscription>> GetActiveWebhooksAsync(CancellationToken cancellationToken = default)
    {
        var activeWebhookIds = await GetActiveWebhooksListAsync(cancellationToken);
        var webhooks = new List<WebhookSubscription>();

        foreach (var id in activeWebhookIds)
        {
            var webhook = await GetWebhookAsync(id, cancellationToken);
            if (webhook != null && webhook.IsActive)
            {
                webhooks.Add(webhook);
            }
        }

        return webhooks;
    }

    public async Task<WebhookDeliveryResult> SendWebhookAsync(
        WebhookEventType eventType,
        object data,
        CancellationToken cancellationToken = default)
    {
        var activeWebhooks = await GetActiveWebhooksAsync(cancellationToken);
        var relevantWebhooks = activeWebhooks
            .Where(w => w.Events.Contains(eventType))
            .ToList();

        _logger.LogInformation("Sending {EventType} webhook to {Count} subscribers",
            eventType, relevantWebhooks.Count);

        var results = new List<WebhookDeliveryResult>();

        foreach (var webhook in relevantWebhooks)
        {
            var result = await DeliverWebhookAsync(webhook, eventType, data, cancellationToken);
            results.Add(result);

            // Update webhook status based on delivery result
            await UpdateWebhookStatusAsync(webhook, result, cancellationToken);
        }

        // Return aggregate result
        return new WebhookDeliveryResult
        {
            Success = results.All(r => r.Success),
            StatusCode = results.Any() ? results.First().StatusCode : 0,
            Duration = TimeSpan.FromMilliseconds(results.Sum(r => r.Duration.TotalMilliseconds)),
            DeliveredAt = DateTime.UtcNow,
            AttemptNumber = 1
        };
    }

    public Task<bool> ValidateWebhookSignatureAsync(string payload, string signature, string secret)
    {
        var expectedSignature = ComputeSignature(payload, secret);
        var isValid = string.Equals(signature, expectedSignature, StringComparison.OrdinalIgnoreCase);
        return Task.FromResult(isValid);
    }

    private async Task<WebhookDeliveryResult> DeliverWebhookAsync(
        WebhookSubscription subscription,
        WebhookEventType eventType,
        object data,
        CancellationToken cancellationToken)
    {
        var startTime = DateTime.UtcNow;
        var payload = new WebhookPayload
        {
            EventType = eventType,
            Data = data,
            Metadata = new Dictionary<string, string>
            {
                ["subscription_id"] = subscription.Id.ToString(),
                ["delivery_attempt"] = "1"
            }
        };

        var json = JsonSerializer.Serialize(payload, _jsonOptions);
        var signature = ComputeSignature(json, subscription.Secret);

        using var httpClient = _httpClientFactory.CreateClient();
        httpClient.Timeout = TimeSpan.FromSeconds(subscription.TimeoutSeconds);

        using var request = new HttpRequestMessage(HttpMethod.Post, subscription.Url)
        {
            Content = new StringContent(json, Encoding.UTF8, "application/json")
        };

        // Add webhook headers
        request.Headers.Add("X-Webhook-Signature", signature);
        request.Headers.Add("X-Webhook-Event", eventType.ToString());
        request.Headers.Add("X-Webhook-Id", payload.Id.ToString());
        request.Headers.Add("X-Webhook-Timestamp", payload.Timestamp.ToString("O"));

        // Add custom headers from subscription
        foreach (var header in subscription.Headers)
        {
            request.Headers.TryAddWithoutValidation(header.Key, header.Value);
        }

        try
        {
            var response = await _retryPolicy.ExecuteAsync(async () =>
                await httpClient.SendAsync(request, cancellationToken));

            return new WebhookDeliveryResult
            {
                Success = response.IsSuccessStatusCode,
                StatusCode = (int)response.StatusCode,
                Duration = DateTime.UtcNow - startTime,
                DeliveredAt = DateTime.UtcNow,
                AttemptNumber = 1
            };
        }
        catch (TaskCanceledException)
        {
            _logger.LogWarning("Webhook delivery to {Url} timed out", subscription.Url);
            return new WebhookDeliveryResult
            {
                Success = false,
                StatusCode = 408,
                ErrorMessage = "Request timeout",
                Duration = DateTime.UtcNow - startTime,
                DeliveredAt = DateTime.UtcNow,
                AttemptNumber = 1
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Webhook delivery to {Url} failed", subscription.Url);
            return new WebhookDeliveryResult
            {
                Success = false,
                StatusCode = 0,
                ErrorMessage = ex.Message,
                Duration = DateTime.UtcNow - startTime,
                DeliveredAt = DateTime.UtcNow,
                AttemptNumber = 1
            };
        }
    }

    private async Task UpdateWebhookStatusAsync(
        WebhookSubscription webhook,
        WebhookDeliveryResult result,
        CancellationToken cancellationToken)
    {
        webhook.LastDeliveryAt = result.DeliveredAt;

        if (result.Success)
        {
            webhook.ConsecutiveFailures = 0;
        }
        else
        {
            webhook.ConsecutiveFailures++;

            // Disable webhook after too many consecutive failures
            if (webhook.ConsecutiveFailures >= 10)
            {
                _logger.LogWarning("Disabling webhook {WebhookId} after {Failures} consecutive failures",
                    webhook.Id, webhook.ConsecutiveFailures);
                webhook.IsActive = false;
            }
        }

        await _cacheService.SetAsync(
            $"webhook:{webhook.Id}",
            webhook,
            TimeSpan.FromDays(365),
            cancellationToken);
    }

    private async Task<List<Guid>> GetActiveWebhooksListAsync(CancellationToken cancellationToken)
    {
        var list = await _cacheService.GetAsync<List<Guid>>("webhooks:active", cancellationToken);
        return list ?? new List<Guid>();
    }

    private static string GenerateWebhookSecret()
    {
        var bytes = new byte[32];
        using var rng = RandomNumberGenerator.Create();
        rng.GetBytes(bytes);
        return Convert.ToBase64String(bytes);
    }

    private static string ComputeSignature(string payload, string secret)
    {
        var secretBytes = Encoding.UTF8.GetBytes(secret);
        var payloadBytes = Encoding.UTF8.GetBytes(payload);

        using var hmac = new HMACSHA256(secretBytes);
        var hash = hmac.ComputeHash(payloadBytes);
        return Convert.ToBase64String(hash);
    }

    public async Task<IEnumerable<WebhookSubscription>> GetAllWebhooksAsync(CancellationToken cancellationToken = default)
    {
        var activeWebhookIds = await GetActiveWebhooksListAsync(cancellationToken);
        var webhooks = new List<WebhookSubscription>();

        foreach (var id in activeWebhookIds)
        {
            var webhook = await GetWebhookAsync(id, cancellationToken);
            if (webhook != null)
            {
                webhooks.Add(webhook);
            }
        }

        return webhooks;
    }

    public async Task<WebhookDeliveryResult> DeliverWebhookAsync(WebhookSubscription subscription, object payload, CancellationToken cancellationToken = default)
    {
        var jsonPayload = JsonSerializer.Serialize(payload, _jsonOptions);
        var signature = ComputeSignature(jsonPayload, subscription.Secret);
        var startTime = DateTime.UtcNow;

        try
        {
            var client = _httpClientFactory.CreateClient("webhook");
            client.Timeout = TimeSpan.FromSeconds(subscription.TimeoutSeconds);

            var request = new HttpRequestMessage(HttpMethod.Post, subscription.Url)
            {
                Content = new StringContent(jsonPayload, Encoding.UTF8, "application/json")
            };

            request.Headers.Add("X-Webhook-Signature", signature);
            request.Headers.Add("X-Webhook-Timestamp", DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString());

            foreach (var header in subscription.Headers)
            {
                request.Headers.TryAddWithoutValidation(header.Key, header.Value);
            }

            var response = await _retryPolicy.ExecuteAsync(async () =>
                await client.SendAsync(request, cancellationToken));

            var responseBody = await response.Content.ReadAsStringAsync(cancellationToken);
            var duration = DateTime.UtcNow - startTime;

            return new WebhookDeliveryResult
            {
                Success = response.IsSuccessStatusCode,
                StatusCode = (int)response.StatusCode,
                ResponseBody = responseBody,
                ResponseTime = (int)duration.TotalMilliseconds,
                Error = response.IsSuccessStatusCode ? null : $"HTTP {response.StatusCode}",
                Duration = duration,
                DeliveredAt = DateTime.UtcNow,
                AttemptNumber = 1
            };
        }
        catch (Exception ex)
        {
            return new WebhookDeliveryResult
            {
                Success = false,
                StatusCode = 0,
                Error = ex.Message,
                ResponseTime = (int)(DateTime.UtcNow - startTime).TotalMilliseconds,
                Duration = DateTime.UtcNow - startTime,
                DeliveredAt = DateTime.UtcNow,
                AttemptNumber = 1
            };
        }
    }

    public bool ValidateWebhookSignature(string payload, string signature, string secret)
    {
        var computedSignature = ComputeSignature(payload, secret);
        return string.Equals(signature, computedSignature, StringComparison.Ordinal);
    }
}