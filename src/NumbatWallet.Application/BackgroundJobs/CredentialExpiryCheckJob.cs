using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using NumbatWallet.Domain.Interfaces;

namespace NumbatWallet.Application.BackgroundJobs;

/// <summary>
/// Background job that checks for expiring credentials and updates their status.
/// Runs every hour.
/// </summary>
public class CredentialExpiryCheckJob(
    IServiceScopeFactory scopeFactory,
    ILogger<CredentialExpiryCheckJob> logger) : BackgroundService
{
    private static readonly TimeSpan Interval = TimeSpan.FromHours(1);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("CredentialExpiryCheckJob started");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await CheckExpiredCredentialsAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error checking credential expiry");
            }

            await Task.Delay(Interval, stoppingToken);
        }
    }

    private async Task CheckExpiredCredentialsAsync(CancellationToken cancellationToken)
    {
        using var scope = scopeFactory.CreateScope();
        var credentialRepository = scope.ServiceProvider.GetRequiredService<ICredentialRepository>();

        var expiredCredentials = await credentialRepository.GetExpiredCredentialsAsync(cancellationToken);
        var credentialList = expiredCredentials.ToList();

        if (credentialList.Count == 0)
        {
            return;
        }

        logger.LogInformation("Found {Count} expired credentials to process", credentialList.Count);

        foreach (var credential in credentialList)
        {
            // SetExpiry handles status transition to Expired when credential.IsExpired()
            if (credential.IsExpired() && credential.Status == SharedKernel.Enums.CredentialStatus.Active)
            {
                credential.SetExpiry(credential.ExpiresAt!.Value);
                await credentialRepository.UpdateAsync(credential, cancellationToken);
            }
        }

        logger.LogInformation("Processed {Count} expired credentials", credentialList.Count);
    }
}
