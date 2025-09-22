using HotChocolate;
using HotChocolate.Execution;
using HotChocolate.Subscriptions;
using HotChocolate.Types;
using NumbatWallet.Application.Commands.Credentials;
using NumbatWallet.Application.Interfaces;

namespace NumbatWallet.Web.Api.GraphQL.Subscriptions;

/// <summary>
/// GraphQL subscriptions for real-time bulk operation progress
/// POA: Issue #187 - Real-time progress updates via GraphQL subscriptions
/// </summary>
[ExtendObjectType("Subscription")]
public class BulkOperationSubscriptions
{
    /// <summary>
    /// Subscribe to bulk operation progress updates
    /// </summary>
    [Subscribe]
    [Topic("OnBulkOperationStarted_{operationId}")]
    [GraphQLDescription("Subscribe to real-time progress updates for a bulk operation")]
    public ProgressUpdate BulkOperationProgress(
        [EventMessage] BulkOperationResult operationResult,
        string operationId)
    {
        return new ProgressUpdate
        {
            OperationId = operationResult.OperationId,
            OperationName = $"Bulk Operation {operationResult.OperationId}",
            Status = ConvertStatus(operationResult.Status),
            ProcessedItems = operationResult.ProcessedCount,
            TotalItems = operationResult.TotalCount,
            CurrentMessage = $"Processed {operationResult.ProcessedCount} of {operationResult.TotalCount} items",
            StartTime = DateTime.UtcNow.AddMinutes(-5), // Mock start time
            EndTime = operationResult.CompletedAt
        };
    }

    /// <summary>
    /// Subscribe to all bulk operation starts for a tenant
    /// </summary>
    [Subscribe]
    [GraphQLDescription("Subscribe to all bulk operations starting for the current tenant")]
    public async IAsyncEnumerable<BulkOperationStartedEvent> OnBulkOperationStarted(
        [Service] ITopicEventReceiver eventReceiver,
        CancellationToken cancellationToken)
    {
        var stream = await eventReceiver.SubscribeAsync<BulkOperationStartedEvent>(
            "BulkOperationStarted",
            cancellationToken);

        await foreach (var @event in stream.ReadEventsAsync().WithCancellation(cancellationToken))
        {
            yield return @event;
        }
    }

    /// <summary>
    /// Subscribe to bulk operation completions
    /// </summary>
    [Subscribe]
    [GraphQLDescription("Subscribe to bulk operation completion events")]
    public async IAsyncEnumerable<BulkOperationCompletedEvent> OnBulkOperationCompleted(
        [Service] ITopicEventReceiver eventReceiver,
        CancellationToken cancellationToken)
    {
        var stream = await eventReceiver.SubscribeAsync<BulkOperationCompletedEvent>(
            "BulkOperationCompleted",
            cancellationToken);

        await foreach (var @event in stream.ReadEventsAsync().WithCancellation(cancellationToken))
        {
            yield return @event;
        }
    }

    /// <summary>
    /// Subscribe to bulk operation errors
    /// </summary>
    [Subscribe]
    [GraphQLDescription("Subscribe to bulk operation error events")]
    public async IAsyncEnumerable<BulkOperationErrorEvent> OnBulkOperationError(
        [Service] ITopicEventReceiver eventReceiver,
        string? operationId,
        CancellationToken cancellationToken)
    {
        var topic = string.IsNullOrEmpty(operationId)
            ? "BulkOperationError"
            : $"BulkOperationError_{operationId}";

        var stream = await eventReceiver.SubscribeAsync<BulkOperationErrorEvent>(
            topic,
            cancellationToken);

        await foreach (var @event in stream.ReadEventsAsync().WithCancellation(cancellationToken))
        {
            yield return @event;
        }
    }

    private static ProgressStatus ConvertStatus(BulkOperationStatus status)
    {
        return status switch
        {
            BulkOperationStatus.Pending => ProgressStatus.NotStarted,
            BulkOperationStatus.InProgress => ProgressStatus.InProgress,
            BulkOperationStatus.Completed => ProgressStatus.Completed,
            BulkOperationStatus.Failed => ProgressStatus.Failed,
            BulkOperationStatus.PartiallyCompleted => ProgressStatus.Completed,
            _ => ProgressStatus.InProgress
        };
    }
}

// Event and result types for subscriptions
public class BulkOperationResult
{
    public string OperationId { get; set; } = string.Empty;
    public BulkOperationStatus Status { get; set; }
    public int TotalCount { get; set; }
    public int ProcessedCount { get; set; }
    public int SuccessCount { get; set; }
    public int FailureCount { get; set; }
    public DateTime? CompletedAt { get; set; }
}

public enum BulkOperationStatus
{
    Pending,
    InProgress,
    Completed,
    Failed,
    PartiallyCompleted
}

public class BulkOperationStartedEvent
{
    public string OperationId { get; set; } = string.Empty;
    public string OperationType { get; set; } = string.Empty;
    public int TotalCount { get; set; }
    public DateTime StartedAt { get; set; }
    public Dictionary<string, object> Metadata { get; set; } = new();
}

public class BulkOperationCompletedEvent
{
    public string OperationId { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public int TotalCount { get; set; }
    public int SuccessCount { get; set; }
    public int FailureCount { get; set; }
    public DateTime CompletedAt { get; set; }
    public TimeSpan Duration { get; set; }
    public Dictionary<string, object> Statistics { get; set; } = new();
}

public class BulkOperationErrorEvent
{
    public string OperationId { get; set; } = string.Empty;
    public int ErrorIndex { get; set; }
    public string ErrorCode { get; set; } = string.Empty;
    public string ErrorMessage { get; set; } = string.Empty;
    public DateTime OccurredAt { get; set; }
    public Dictionary<string, object> Context { get; set; } = new();
}