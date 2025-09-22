using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using NumbatWallet.Application.Commands.Credentials;
using System.Collections.Concurrent;

namespace NumbatWallet.Web.Api.Hubs;

/// <summary>
/// SignalR hub for real-time progress tracking of bulk operations
/// POA: Issue #187 - Real-time progress updates via WebSocket
/// </summary>
[Authorize]
public class ProgressHub : Hub
{
    private readonly ILogger<ProgressHub> _logger;
    private static readonly ConcurrentDictionary<string, HashSet<string>> _operationConnections = new();
    private static readonly ConcurrentDictionary<string, ProgressUpdate> _latestProgress = new();

    public ProgressHub(ILogger<ProgressHub> logger)
    {
        _logger = logger;
    }

    public override async Task OnConnectedAsync()
    {
        _logger.LogInformation("Client connected: {ConnectionId}", Context.ConnectionId);
        await Clients.Caller.SendAsync("Connected", Context.ConnectionId);
        await base.OnConnectedAsync();
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        _logger.LogInformation("Client disconnected: {ConnectionId}", Context.ConnectionId);

        // Remove connection from all operation groups
        foreach (var operation in _operationConnections.ToList())
        {
            operation.Value.Remove(Context.ConnectionId);
            if (operation.Value.Count == 0)
            {
                _operationConnections.TryRemove(operation.Key, out _);
            }
        }

        await base.OnDisconnectedAsync(exception);
    }

    /// <summary>
    /// Subscribe to progress updates for a specific operation
    /// </summary>
    public async Task SubscribeToOperation(string operationId)
    {
        _logger.LogInformation("Client {ConnectionId} subscribing to operation {OperationId}",
            Context.ConnectionId, operationId);

        // Add to SignalR group
        await Groups.AddToGroupAsync(Context.ConnectionId, $"operation-{operationId}");

        // Track connection
        _operationConnections.AddOrUpdate(operationId,
            new HashSet<string> { Context.ConnectionId },
            (key, set) =>
            {
                set.Add(Context.ConnectionId);
                return set;
            });

        // Send latest progress if available
        if (_latestProgress.TryGetValue(operationId, out var progress))
        {
            await Clients.Caller.SendAsync("ProgressUpdate", progress);
        }

        await Clients.Caller.SendAsync("SubscriptionConfirmed", operationId);
    }

    /// <summary>
    /// Unsubscribe from progress updates for a specific operation
    /// </summary>
    public async Task UnsubscribeFromOperation(string operationId)
    {
        _logger.LogInformation("Client {ConnectionId} unsubscribing from operation {OperationId}",
            Context.ConnectionId, operationId);

        // Remove from SignalR group
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"operation-{operationId}");

        // Remove from tracking
        if (_operationConnections.TryGetValue(operationId, out var connections))
        {
            connections.Remove(Context.ConnectionId);
            if (connections.Count == 0)
            {
                _operationConnections.TryRemove(operationId, out _);
                _latestProgress.TryRemove(operationId, out _);
            }
        }

        await Clients.Caller.SendAsync("UnsubscriptionConfirmed", operationId);
    }

    /// <summary>
    /// Get current progress for an operation
    /// </summary>
    public async Task GetOperationProgress(string operationId)
    {
        if (_latestProgress.TryGetValue(operationId, out var progress))
        {
            await Clients.Caller.SendAsync("ProgressUpdate", progress);
        }
        else
        {
            await Clients.Caller.SendAsync("OperationNotFound", operationId);
        }
    }

    /// <summary>
    /// Internal method to broadcast progress updates (called by the progress service)
    /// </summary>
    public static async Task BroadcastProgressAsync(
        IHubContext<ProgressHub> hubContext,
        string operationId,
        ProgressUpdate progress)
    {
        // Store latest progress
        _latestProgress.AddOrUpdate(operationId, progress, (key, old) => progress);

        // Broadcast to all clients in the operation group
        await hubContext.Clients.Group($"operation-{operationId}").SendAsync("ProgressUpdate", progress);

        // Clean up completed operations after a delay
        if (progress.Status == "Completed" || progress.Status == "Failed" || progress.Status == "Cancelled")
        {
            _ = Task.Delay(TimeSpan.FromMinutes(5)).ContinueWith(_ =>
            {
                _latestProgress.TryRemove(operationId, out _);
                _operationConnections.TryRemove(operationId, out _);
            });
        }
    }
}

/// <summary>
/// Service implementation for progress notifications
/// </summary>
public class SignalRProgressNotificationService : IProgressNotificationService
{
    private readonly IHubContext<ProgressHub> _hubContext;
    private readonly ILogger<SignalRProgressNotificationService> _logger;

    public SignalRProgressNotificationService(
        IHubContext<ProgressHub> hubContext,
        ILogger<SignalRProgressNotificationService> logger)
    {
        _hubContext = hubContext;
        _logger = logger;
    }

    public async Task NotifyProgressAsync(
        string operationId,
        ProgressUpdate update,
        CancellationToken cancellationToken = default)
    {
        try
        {
            await ProgressHub.BroadcastProgressAsync(_hubContext, operationId, update);

            _logger.LogDebug("Progress notification sent for operation {OperationId}: {Percent}% complete",
                operationId, update.PercentComplete);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send progress notification for operation {OperationId}",
                operationId);
        }
    }
}