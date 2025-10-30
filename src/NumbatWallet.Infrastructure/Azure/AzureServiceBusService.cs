using System.Text.Json;
using Microsoft.Extensions.Logging;

namespace NumbatWallet.Infrastructure.Azure;

/// <summary>
/// Azure Service Bus implementation for messaging
/// POA: Phase 3 - Service Bus integration
/// </summary>
public class AzureServiceBusService : IAzureServiceBusService
{
    private readonly string _connectionString;
    private readonly ILogger<AzureServiceBusService> _logger;

    public AzureServiceBusService(
        string connectionString,
        ILogger<AzureServiceBusService> logger)
    {
        _connectionString = connectionString;
        _logger = logger;
    }

    public async Task SendMessageAsync(
        string queueName,
        string message,
        CancellationToken cancellationToken = default)
    {
        try
        {
            // Note: Full implementation would use Azure.Messaging.ServiceBus
            // For POA, we'll use a simplified approach
            _logger.LogInformation(
                "Sending message to queue {QueueName}: {Message}",
                queueName, message);

            // Simulated send
            await Task.Delay(10, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send message to queue {QueueName}", queueName);
            throw;
        }
    }

    public async Task<string?> ReceiveMessageAsync(
        string queueName,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Receiving message from queue {QueueName}", queueName);

            // Simulated receive
            await Task.Delay(10, cancellationToken);
            return null; // No messages in POA implementation
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to receive message from queue {QueueName}", queueName);
            throw;
        }
    }

    public async Task PublishEventAsync(
        string topicName,
        object eventData,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var json = JsonSerializer.Serialize(eventData);

            _logger.LogInformation(
                "Publishing event to topic {TopicName}: {Event}",
                topicName, json);

            // Simulated publish
            await Task.Delay(10, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to publish event to topic {TopicName}", topicName);
            throw;
        }
    }
}

/// <summary>
/// Mock implementation for development/testing
/// </summary>
public class MockAzureServiceBusService : IAzureServiceBusService
{
    private readonly Dictionary<string, Queue<string>> _queues = new();
    private readonly Dictionary<string, List<object>> _topics = new();
    private readonly ILogger<MockAzureServiceBusService> _logger;

    public MockAzureServiceBusService(ILogger<MockAzureServiceBusService> logger)
    {
        _logger = logger;
    }

    public Task SendMessageAsync(
        string queueName,
        string message,
        CancellationToken cancellationToken = default)
    {
        if (!_queues.ContainsKey(queueName))
        {
            _queues[queueName] = new Queue<string>();
        }

        _queues[queueName].Enqueue(message);
        _logger.LogInformation("Mock sent message to queue {QueueName}", queueName);

        return Task.CompletedTask;
    }

    public Task<string?> ReceiveMessageAsync(
        string queueName,
        CancellationToken cancellationToken = default)
    {
        if (_queues.TryGetValue(queueName, out var queue) && queue.Count > 0)
        {
            var message = queue.Dequeue();
            _logger.LogInformation("Mock received message from queue {QueueName}", queueName);
            return Task.FromResult<string?>(message);
        }

        return Task.FromResult<string?>(null);
    }

    public Task PublishEventAsync(
        string topicName,
        object eventData,
        CancellationToken cancellationToken = default)
    {
        if (!_topics.ContainsKey(topicName))
        {
            _topics[topicName] = new List<object>();
        }

        _topics[topicName].Add(eventData);
        _logger.LogInformation("Mock published event to topic {TopicName}", topicName);

        return Task.CompletedTask;
    }
}