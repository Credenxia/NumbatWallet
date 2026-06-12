using System.Threading.Channels;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using NumbatWallet.Application.Interfaces;

namespace NumbatWallet.Application.BackgroundJobs;

/// <summary>
/// Message type for queued notifications.
/// </summary>
public record NotificationMessage(
    Guid UserId,
    string Title,
    string Body,
    bool IsUrgent = false);

/// <summary>
/// Background job that processes queued notifications from a Channel.
/// Runs continuously, processing notifications as they arrive.
/// </summary>
public class NotificationProcessorJob(
    Channel<NotificationMessage> notificationChannel,
    IServiceScopeFactory scopeFactory,
    ILogger<NotificationProcessorJob> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("NotificationProcessorJob started");

        await foreach (var message in notificationChannel.Reader.ReadAllAsync(stoppingToken))
        {
            try
            {
                using var scope = scopeFactory.CreateScope();
                var notificationService = scope.ServiceProvider.GetRequiredService<INotificationService>();

                if (message.IsUrgent)
                {
                    await notificationService.SendUrgentNotificationAsync(
                        message.UserId, message.Title, message.Body, stoppingToken);
                }
                else
                {
                    await notificationService.SendNotificationAsync(
                        message.UserId, message.Title, message.Body, stoppingToken);
                }

                logger.LogDebug("Processed notification for user {UserId}: {Title}", message.UserId, message.Title);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error processing notification for user {UserId}", message.UserId);
            }
        }
    }
}
