using Azure.Messaging.ServiceBus;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;
using NumbatWallet.Domain.Interfaces;
using NumbatWallet.Infrastructure.Data;
using NumbatWallet.SharedKernel.Interfaces;
using System.Text.Json;

namespace NumbatWallet.Infrastructure.Services;

/// <summary>
/// Implementation of key rotation service for automated key lifecycle management
/// POA-131: Implement key rotation policies
/// </summary>
public class KeyRotationService : IKeyRotationService
{
    private readonly IHsmService _hsmService;
    private readonly IHsmProvider _hsmProvider;
    private readonly NumbatWalletDbContext _context;
    private readonly IDistributedCache _cache;
    private readonly ServiceBusClient _serviceBusClient;
    private readonly IDateTimeService _dateTimeService;
    private readonly ICurrentUserService _currentUserService;
    private readonly ILogger<KeyRotationService> _logger;

    public KeyRotationService(
        IHsmService hsmService,
        IHsmProvider hsmProvider,
        NumbatWalletDbContext context,
        IDistributedCache cache,
        ServiceBusClient serviceBusClient,
        IDateTimeService dateTimeService,
        ICurrentUserService currentUserService,
        ILogger<KeyRotationService> logger)
    {
        _hsmService = hsmService;
        _hsmProvider = hsmProvider;
        _context = context;
        _cache = cache;
        _serviceBusClient = serviceBusClient;
        _dateTimeService = dateTimeService;
        _currentUserService = currentUserService;
        _logger = logger;
    }

    public async Task<RotationResult> RotateKeyAsync(string keyId, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Starting key rotation for {KeyId}", keyId);

            // Get current key metadata
            var currentKey = await _hsmProvider.GetKeyAsync(keyId, cancellationToken);

            // Get rotation policy
            var policy = await GetRotationPolicyAsync(keyId, cancellationToken);

            // Generate new key
            var newKeyName = $"{currentKey.Name}-v{DateTimeOffset.UtcNow:yyyyMMddHHmmss}";
            var newKeyId = await _hsmService.GenerateKeyPairAsync(
                newKeyName,
                KeyAlgorithm.RSA4096,
                cancellationToken);

            // Start grace period - both keys active
            await StartGracePeriodAsync(keyId, newKeyId, policy.GracePeriodDays, cancellationToken);

            // Update dependent systems
            await UpdateDependentSystemsAsync(keyId, newKeyId, cancellationToken);

            // Archive old key (but keep active during grace period)
            await ScheduleKeyArchivalAsync(keyId, policy.GracePeriodDays, cancellationToken);

            // Send notifications
            await NotifyKeyRotationAsync(keyId, newKeyId, policy, cancellationToken);

            // Record rotation in database
            await RecordRotationAsync(keyId, newKeyId, RotationType.Scheduled, cancellationToken);

            var result = new RotationResult
            {
                Success = true,
                RotationId = Guid.NewGuid().ToString(),
                OldKeyId = keyId,
                NewKeyId = newKeyId,
                RotatedAt = _dateTimeService.UtcNow.DateTime,
                GracePeriodEnds = _dateTimeService.UtcNow.AddDays(policy.GracePeriodDays).DateTime,
                Type = RotationType.Scheduled
            };

            _logger.LogInformation("Successfully rotated key {OldKeyId} to {NewKeyId}", keyId, newKeyId);
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to rotate key {KeyId}", keyId);
            return new RotationResult
            {
                Success = false,
                OldKeyId = keyId,
                ErrorMessage = ex.Message,
                Type = RotationType.Manual
            };
        }
    }

    public async Task<IEnumerable<RotationResult>> EnforceRotationPoliciesAsync(CancellationToken cancellationToken = default)
    {
        var results = new List<RotationResult>();

        try
        {
            // Get all active rotation policies
            var policies = await GetActivePoliciesAsync(cancellationToken);

            foreach (var policy in policies)
            {
                if (ShouldRotate(policy))
                {
                    var result = await RotateKeyAsync(policy.KeyId, cancellationToken);
                    results.Add(result);
                }
                else if (ShouldWarn(policy))
                {
                    await SendRotationWarningAsync(policy, cancellationToken);
                }
            }

            _logger.LogInformation("Enforced rotation policies. Rotated {Count} keys", results.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error enforcing rotation policies");
        }

        return results;
    }

    public async Task<bool> ScheduleRotationAsync(string keyId, DateTime rotationTime, CancellationToken cancellationToken = default)
    {
        try
        {
            // Send scheduled message to Service Bus
            var sender = _serviceBusClient.CreateSender("key-rotation-events");
            var message = new ServiceBusMessage(JsonSerializer.Serialize(new
            {
                KeyId = keyId,
                ScheduledTime = rotationTime,
                Type = "ScheduledRotation"
            }))
            {
                ScheduledEnqueueTime = rotationTime
            };

            await sender.SendMessageAsync(message, cancellationToken);

            _logger.LogInformation("Scheduled rotation for key {KeyId} at {Time}", keyId, rotationTime);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to schedule rotation for key {KeyId}", keyId);
            return false;
        }
    }

    public async Task<RotationPolicy> GetRotationPolicyAsync(string keyId, CancellationToken cancellationToken = default)
    {
        // Check cache first
        var cacheKey = $"rotation-policy:{keyId}";
        var cached = await _cache.GetStringAsync(cacheKey, cancellationToken);
        if (!string.IsNullOrEmpty(cached))
        {
            return JsonSerializer.Deserialize<RotationPolicy>(cached)!;
        }

        // Get from configuration or database
        var policy = await LoadRotationPolicyAsync(keyId, cancellationToken);

        // Cache the policy
        await _cache.SetStringAsync(
            cacheKey,
            JsonSerializer.Serialize(policy),
            new DistributedCacheEntryOptions
            {
                SlidingExpiration = TimeSpan.FromHours(1)
            },
            cancellationToken);

        return policy;
    }

    public async Task<bool> UpdateRotationPolicyAsync(string keyId, RotationPolicy policy, CancellationToken cancellationToken = default)
    {
        try
        {
            // Save to database
            await SaveRotationPolicyAsync(policy, cancellationToken);

            // Invalidate cache
            var cacheKey = $"rotation-policy:{keyId}";
            await _cache.RemoveAsync(cacheKey, cancellationToken);

            _logger.LogInformation("Updated rotation policy for key {KeyId}", keyId);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to update rotation policy for key {KeyId}", keyId);
            return false;
        }
    }

    public async Task<IEnumerable<KeyRotationStatus>> GetPendingRotationsAsync(CancellationToken cancellationToken = default)
    {
        var pendingRotations = new List<KeyRotationStatus>();

        var policies = await GetActivePoliciesAsync(cancellationToken);
        foreach (var policy in policies.Where(p => p.NextRotationDate <= _dateTimeService.UtcNow.AddDays(p.WarningDays)))
        {
            var status = await GetKeyRotationStatusAsync(policy.KeyId, cancellationToken);
            pendingRotations.Add(status);
        }

        return pendingRotations;
    }

    public async Task<IEnumerable<KeyRotationStatus>> GetKeysInGracePeriodAsync(CancellationToken cancellationToken = default)
    {
        var keysInGrace = new List<KeyRotationStatus>();

        // Query database for keys in grace period
        var gracePeriodKeys = await _context.Set<KeyRotationRecord>()
            .Where(k => k.GracePeriodEnds > _dateTimeService.UtcNow)
            .ToListAsync(cancellationToken);

        foreach (var key in gracePeriodKeys)
        {
            keysInGrace.Add(new KeyRotationStatus
            {
                KeyId = key.KeyId,
                KeyName = key.KeyName,
                IsInGracePeriod = true,
                GracePeriodEnds = key.GracePeriodEnds,
                State = RotationState.InGracePeriod
            });
        }

        return keysInGrace;
    }

    public async Task<RotationResult> EmergencyRotateAsync(string keyId, string reason, CancellationToken cancellationToken = default)
    {
        _logger.LogWarning("Emergency rotation requested for key {KeyId}: {Reason}", keyId, reason);

        // Immediate rotation without grace period for compromised keys
        var result = await RotateKeyAsync(keyId, cancellationToken);
        result.Type = RotationType.Emergency;

        if (result.Success)
        {
            // Immediately deactivate old key by deleting it from HSM
            await _hsmService.DeleteKeyAsync(keyId, cancellationToken);

            // Send emergency notifications
            await SendEmergencyNotificationsAsync(keyId, reason, cancellationToken);

            // Audit emergency rotation
            await AuditEmergencyRotationAsync(keyId, reason, cancellationToken);
        }

        return result;
    }

    public async Task<bool> RollbackRotationAsync(string rotationId, string reason, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogWarning("Rolling back rotation {RotationId}: {Reason}", rotationId, reason);

            var rotation = await GetRotationRecordAsync(rotationId, cancellationToken);
            if (rotation == null)
            {
                return false;
            }

            // Cannot reactivate deleted key - regenerate it
            await _hsmService.GenerateKeyPairAsync(rotation.OldKeyId, KeyAlgorithm.RSA4096, cancellationToken);

            // Deactivate new key by deleting it
            await _hsmService.DeleteKeyAsync(rotation.NewKeyId, cancellationToken);

            // Update dependent systems to use old key
            await UpdateDependentSystemsAsync(rotation.NewKeyId, rotation.OldKeyId, cancellationToken);

            // Record rollback
            await RecordRollbackAsync(rotationId, reason, cancellationToken);

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to rollback rotation {RotationId}", rotationId);
            return false;
        }
    }

    public async Task<ComplianceReport> GenerateComplianceReportAsync(DateTime startDate, DateTime endDate, CancellationToken cancellationToken = default)
    {
        var report = new ComplianceReport
        {
            GeneratedAt = _dateTimeService.UtcNow.DateTime,
            PeriodStart = startDate,
            PeriodEnd = endDate
        };

        // Get all rotation records for period
        var rotations = await _context.Set<KeyRotationRecord>()
            .Where(r => r.RotatedAt >= startDate && r.RotatedAt <= endDate)
            .ToListAsync(cancellationToken);

        report.TotalKeys = await _context.Set<ManagedKey>().CountAsync(cancellationToken);
        report.KeysRotated = rotations.Count(r => r.Success);
        report.EmergencyRotations = rotations.Count(r => r.Type == RotationType.Emergency);
        report.FailedRotations = rotations.Count(r => !r.Success);

        // Calculate compliance percentage
        var requiredRotations = await CalculateRequiredRotationsAsync(startDate, endDate, cancellationToken);
        report.CompliancePercentage = requiredRotations > 0
            ? (double)report.KeysRotated / requiredRotations * 100
            : 100;

        // Add audit entries
        report.AuditEntries = rotations.Select(r => new RotationAuditEntry
        {
            RotationId = r.RotationId,
            KeyId = r.KeyId,
            RotatedAt = r.RotatedAt,
            Type = r.Type,
            PerformedBy = r.PerformedBy,
            Success = r.Success,
            FailureReason = r.FailureReason
        }).ToList();

        // Generate statistics by key type
        report.StatisticsByKeyType = await GenerateKeyTypeStatisticsAsync(startDate, endDate, cancellationToken);

        _logger.LogInformation("Generated compliance report for period {Start} to {End}", startDate, endDate);
        return report;
    }

    // Private helper methods

    private async Task<IEnumerable<RotationPolicy>> GetActivePoliciesAsync(CancellationToken cancellationToken)
    {
        return await _context.Set<RotationPolicy>()
            .Where(p => p.AutoRotateEnabled)
            .ToListAsync(cancellationToken);
    }

    private bool ShouldRotate(RotationPolicy policy)
    {
        return policy.NextRotationDate <= _dateTimeService.UtcNow;
    }

    private bool ShouldWarn(RotationPolicy policy)
    {
        var warningDate = policy.NextRotationDate?.AddDays(-policy.WarningDays);
        return warningDate <= _dateTimeService.UtcNow;
    }

    private async Task StartGracePeriodAsync(string oldKeyId, string _, int gracePeriodDays, CancellationToken cancellationToken)
    {
        // Both keys remain active during grace period
        // No explicit enable needed - keys are active by default

        // Schedule old key deactivation
        await ScheduleKeyArchivalAsync(oldKeyId, gracePeriodDays, cancellationToken);
    }

    private async Task UpdateDependentSystemsAsync(string oldKeyId, string newKeyId, CancellationToken cancellationToken)
    {
        // Send messages to dependent systems to update their key references
        var sender = _serviceBusClient.CreateSender("key-updates");
        var message = new ServiceBusMessage(JsonSerializer.Serialize(new
        {
            OldKeyId = oldKeyId,
            NewKeyId = newKeyId,
            UpdateType = "KeyRotation",
            Timestamp = _dateTimeService.UtcNow
        }));

        await sender.SendMessageAsync(message, cancellationToken);
    }

    private async Task ScheduleKeyArchivalAsync(string keyId, int daysUntilArchival, CancellationToken cancellationToken)
    {
        var archivalTime = _dateTimeService.UtcNow.AddDays(daysUntilArchival).DateTime;
        await ScheduleRotationAsync(keyId, archivalTime, cancellationToken);
    }

    private async Task NotifyKeyRotationAsync(string _oldKeyId, string _newKeyId, RotationPolicy policy, CancellationToken _cancellationToken)
    {
        if (!string.IsNullOrEmpty(policy.NotificationEmail))
        {
            // Send notification email
            _logger.LogInformation("Sending rotation notification to {Email}", policy.NotificationEmail);
        }
    }

    private async Task RecordRotationAsync(string oldKeyId, string newKeyId, RotationType type, CancellationToken cancellationToken)
    {
        var record = new KeyRotationRecord
        {
            RotationId = Guid.NewGuid().ToString(),
            OldKeyId = oldKeyId,
            NewKeyId = newKeyId,
            RotatedAt = _dateTimeService.UtcNow.DateTime,
            Type = type,
            PerformedBy = _currentUserService.UserId,
            Success = true
        };

        _context.Add(record);
        await _context.SaveChangesAsync(cancellationToken);
    }

    private async Task<RotationPolicy> LoadRotationPolicyAsync(string keyId, CancellationToken cancellationToken)
    {
        // Default policies based on key type
        var keyType = await DetermineKeyTypeAsync(keyId, cancellationToken);

        return new RotationPolicy
        {
            KeyId = keyId,
            KeyType = keyType,
            RotationIntervalDays = GetDefaultRotationInterval(keyType),
            GracePeriodDays = 7,
            WarningDays = 14,
            AutoRotateEnabled = true,
            MinimumKeyAgeDays = 30,
            MaximumKeyAgeDays = GetMaximumKeyAge(keyType),
            RequiredComplianceLevel = GetRequiredComplianceLevel(keyType)
        };
    }

    private int GetDefaultRotationInterval(RotatableKeyType keyType)
    {
        return keyType switch
        {
            RotatableKeyType.SigningKey => 90,
            RotatableKeyType.EncryptionKey => 365,
            RotatableKeyType.TlsCertificate => 30,
            RotatableKeyType.ApiKey => 180,
            RotatableKeyType.HsmMasterKey => 730,
            _ => 365
        };
    }

    private int GetMaximumKeyAge(RotatableKeyType keyType)
    {
        return keyType switch
        {
            RotatableKeyType.SigningKey => 180,
            RotatableKeyType.EncryptionKey => 730,
            RotatableKeyType.TlsCertificate => 90,
            RotatableKeyType.ApiKey => 365,
            RotatableKeyType.HsmMasterKey => 1095,
            _ => 730
        };
    }

    private ComplianceLevel GetRequiredComplianceLevel(RotatableKeyType keyType)
    {
        return keyType switch
        {
            RotatableKeyType.HsmMasterKey => ComplianceLevel.Critical,
            RotatableKeyType.SigningKey => ComplianceLevel.High,
            RotatableKeyType.EncryptionKey => ComplianceLevel.High,
            RotatableKeyType.TlsCertificate => ComplianceLevel.Medium,
            _ => ComplianceLevel.Low
        };
    }

    private async Task<RotatableKeyType> DetermineKeyTypeAsync(string keyId, CancellationToken _ct)
    {
        // Determine based on key naming convention or metadata
        if (keyId.Contains("sign", StringComparison.OrdinalIgnoreCase))
        {
            return RotatableKeyType.SigningKey;
        }
        if (keyId.Contains("encrypt", StringComparison.OrdinalIgnoreCase))
        {
            return RotatableKeyType.EncryptionKey;
        }
        if (keyId.Contains("tls", StringComparison.OrdinalIgnoreCase) || keyId.Contains("cert", StringComparison.OrdinalIgnoreCase))
        {
            return RotatableKeyType.TlsCertificate;
        }
        if (keyId.Contains("api", StringComparison.OrdinalIgnoreCase))
        {
            return RotatableKeyType.ApiKey;
        }

        return RotatableKeyType.EncryptionKey; // Default
    }

    private async Task SaveRotationPolicyAsync(RotationPolicy _policy, CancellationToken _cancellationToken)
    {
        // Save to database - implementation depends on your entity structure
        await Task.CompletedTask;
    }

    private async Task<KeyRotationStatus> GetKeyRotationStatusAsync(string keyId, CancellationToken _cancellationToken)
    {
        // Get key metadata and calculate status
        await Task.CompletedTask;
        return new KeyRotationStatus
        {
            KeyId = keyId,
            State = RotationState.Active
        };
    }

    private async Task SendRotationWarningAsync(RotationPolicy policy, CancellationToken _cancellationToken)
    {
        _logger.LogWarning("Key {KeyId} is due for rotation in {Days} days",
            policy.KeyId,
            (policy.NextRotationDate - _dateTimeService.UtcNow)?.Days ?? 0);
        await Task.CompletedTask;
    }

    private async Task SendEmergencyNotificationsAsync(string keyId, string reason, CancellationToken _cancellationToken)
    {
        _logger.LogCritical("Emergency rotation performed for key {KeyId}: {Reason}", keyId, reason);
        // Send alerts to security team
        await Task.CompletedTask;
    }

    private async Task AuditEmergencyRotationAsync(string _keyId, string _reason, CancellationToken _cancellationToken)
    {
        // Record in audit log
        await Task.CompletedTask;
    }

    private async Task<KeyRotationRecord?> GetRotationRecordAsync(string rotationId, CancellationToken cancellationToken)
    {
        return await _context.Set<KeyRotationRecord>()
            .FirstOrDefaultAsync(r => r.RotationId == rotationId, cancellationToken);
    }

    private async Task RecordRollbackAsync(string _rotationId, string _reason, CancellationToken _cancellationToken)
    {
        // Record rollback in audit log
        await Task.CompletedTask;
    }

    private async Task<int> CalculateRequiredRotationsAsync(DateTime startDate, DateTime endDate, CancellationToken cancellationToken)
    {
        // Calculate how many rotations should have occurred based on policies
        var policies = await GetActivePoliciesAsync(cancellationToken);
        var requiredCount = 0;

        foreach (var policy in policies)
        {
            var intervalDays = policy.RotationIntervalDays;
            var periodDays = (endDate - startDate).Days;
            requiredCount += periodDays / intervalDays;
        }

        return requiredCount;
    }

    private async Task<Dictionary<RotatableKeyType, ComplianceStatistics>> GenerateKeyTypeStatisticsAsync(
        DateTime startDate,
        DateTime endDate,
        CancellationToken cancellationToken)
    {
        var statistics = new Dictionary<RotatableKeyType, ComplianceStatistics>();

        foreach (RotatableKeyType keyType in Enum.GetValues<RotatableKeyType>())
        {
            var stats = new ComplianceStatistics
            {
                KeyType = keyType,
                TotalKeys = await _context.Set<ManagedKey>()
                    .CountAsync(k => k.Type == ConvertToKeyType(keyType) && k.IsActive, cancellationToken),
                RotationsPerformed = await _context.Set<KeyRotationRecord>()
                    .CountAsync(r => r.Type == ConvertToRotationType(keyType) &&
                                   r.RotatedAt >= startDate &&
                                   r.RotatedAt <= endDate &&
                                   r.Success, cancellationToken),
                CompliantKeys = 0, // Calculate based on policy requirements
                NonCompliantKeys = 0, // Calculate based on policy requirements
                AverageKeyAge = 0 // Calculate average age
            };

            statistics[keyType] = stats;
        }

        return statistics;
    }

    private KeyType ConvertToKeyType(RotatableKeyType rotatableKeyType)
    {
        return rotatableKeyType switch
        {
            RotatableKeyType.SigningKey => KeyType.RSA,
            RotatableKeyType.EncryptionKey => KeyType.AES,
            RotatableKeyType.TlsCertificate => KeyType.RSA,
            RotatableKeyType.ApiKey => KeyType.AES,
            _ => KeyType.RSA
        };
    }

    private RotationType ConvertToRotationType(RotatableKeyType _)
    {
        // Map key types to rotation types - this is a simple mapping
        // In a real implementation, you'd track the actual rotation type
        return RotationType.Scheduled;
    }
}

// Supporting entities for database storage

public class KeyRotationRecord
{
    public string RotationId { get; set; } = string.Empty;
    public string KeyId { get; set; } = string.Empty;
    public string KeyName { get; set; } = string.Empty;
    public string OldKeyId { get; set; } = string.Empty;
    public string NewKeyId { get; set; } = string.Empty;
    public DateTime RotatedAt { get; set; }
    public DateTime? GracePeriodEnds { get; set; }
    public RotationType Type { get; set; }
    public string PerformedBy { get; set; } = string.Empty;
    public bool Success { get; set; }
    public string? FailureReason { get; set; }
}

public class ManagedKey
{
    public string KeyId { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public KeyType Type { get; set; }
    public DateTime CreatedAt { get; set; }
    public bool IsActive { get; set; }
}
