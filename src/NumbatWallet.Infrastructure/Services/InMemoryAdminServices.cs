using System.Collections.Concurrent;
using System.Security.Cryptography;
using System.Text.Json;
using NumbatWallet.Application.DTOs;
using NumbatWallet.Application.Interfaces;

namespace NumbatWallet.Infrastructure.Services;

// ---------------------------------------------------------------------------
// POA in-memory stub implementations backing the Admin GraphQL surface.
//
// These services return MOCK data held in process memory. They exist so the
// Admin GraphQL fields (featureFlags, configurations, backups, reports, admin
// users, key rotation, maintenance) are live and exercisable during the POA
// phase. Real implementations (database/Key Vault/backup tooling) are a
// separate epic. All classes are singletons with no scoped dependencies, so
// they are safe to inject into HotChocolate resolvers via [Service].
// ---------------------------------------------------------------------------

/// <summary>POA stub: in-memory feature flags (mock data, state survives for process lifetime only).</summary>
public sealed class InMemoryFeatureFlagService : IFeatureFlagService
{
    private readonly ConcurrentDictionary<string, FeatureFlagDto> _flags = new(StringComparer.OrdinalIgnoreCase);

    public InMemoryFeatureFlagService()
    {
        Seed("graphql-admin", "Admin GraphQL surface", enabled: true);
        Seed("bulk-operations", "Bulk credential operations", enabled: true);
        Seed("credential-sharing", "Credential sharing via email links", enabled: true);
        Seed("offline-verification", "Offline credential verification", enabled: false);
    }

    private void Seed(string id, string description, bool enabled) =>
        _flags[id] = new FeatureFlagDto
        {
            Id = id,
            Name = id,
            Description = description,
            Enabled = enabled,
            EnabledSince = enabled ? DateTime.UtcNow : null,
            DisabledSince = enabled ? null : DateTime.UtcNow
        };

    public Task<bool> IsFeatureEnabledAsync(string featureName, CancellationToken cancellationToken = default) =>
        Task.FromResult(_flags.TryGetValue(featureName, out var flag) && flag.Enabled);

    public Task EnableFeatureAsync(string featureName, CancellationToken cancellationToken = default)
    {
        ToggleFlagAsync(featureName, true, cancellationToken);
        return Task.CompletedTask;
    }

    public Task DisableFeatureAsync(string featureName, CancellationToken cancellationToken = default)
    {
        ToggleFlagAsync(featureName, false, cancellationToken);
        return Task.CompletedTask;
    }

    public async Task<bool> ToggleFeatureAsync(string featureName, CancellationToken cancellationToken = default)
    {
        var current = await IsFeatureEnabledAsync(featureName, cancellationToken);
        var flag = await ToggleFlagAsync(featureName, !current, cancellationToken);
        return flag.Enabled;
    }

    public Task<Dictionary<string, bool>> GetAllFeaturesAsync(CancellationToken cancellationToken = default) =>
        Task.FromResult(_flags.ToDictionary(kvp => kvp.Key, kvp => kvp.Value.Enabled));

    public Task<bool> IsFeatureEnabledForUserAsync(string featureName, string userId, CancellationToken cancellationToken = default) =>
        IsFeatureEnabledAsync(featureName, cancellationToken); // POA: no per-user targeting

    public Task<FeatureFlagDto> ToggleFlagAsync(string flagId, bool enabled, CancellationToken cancellationToken = default)
    {
        var flag = _flags.GetOrAdd(flagId, id => new FeatureFlagDto { Id = id, Name = id });
        flag.Enabled = enabled;
        if (enabled)
        {
            flag.EnabledSince = DateTime.UtcNow;
            flag.DisabledSince = null;
        }
        else
        {
            flag.DisabledSince = DateTime.UtcNow;
            flag.EnabledSince = null;
        }

        return Task.FromResult(flag);
    }

    public Task<List<FeatureFlagDto>> GetAllFlagsAsync(CancellationToken cancellationToken = default) =>
        Task.FromResult(_flags.Values.OrderBy(f => f.Id, StringComparer.OrdinalIgnoreCase).ToList());
}

/// <summary>POA stub: in-memory configuration store (mock data).</summary>
public sealed class InMemoryConfigurationService : IConfigurationService
{
    private readonly ConcurrentDictionary<string, ConfigurationDto> _entries = new(StringComparer.OrdinalIgnoreCase);

    public InMemoryConfigurationService()
    {
        Seed("Wallet:MaxCredentials", "100", "Development");
        Seed("Credentials:DefaultValidityDays", "365", "Development");
        Seed("Wallet:MaxCredentials", "50", "Production");
    }

    private static string MakeKey(string environment, string key) => $"{environment}::{key}";

    private void Seed(string key, string value, string environment) =>
        _entries[MakeKey(environment, key)] = new ConfigurationDto
        {
            Key = key,
            Value = value,
            Environment = environment,
            UpdatedAt = DateTime.UtcNow,
            UpdatedBy = "seed"
        };

    public Task<string?> GetConfigurationAsync(string key, CancellationToken cancellationToken = default) =>
        Task.FromResult(_entries.Values
            .Where(e => string.Equals(e.Key, key, StringComparison.OrdinalIgnoreCase))
            .Select(e => e.Value)
            .FirstOrDefault());

    public Task SetConfigurationAsync(string key, string value, CancellationToken cancellationToken = default) =>
        UpdateConfigurationAsync(key, value, "Production", cancellationToken);

    public Task<ConfigurationDto> UpdateConfigurationAsync(string key, string value, string environment, CancellationToken cancellationToken = default)
    {
        var entry = new ConfigurationDto
        {
            Key = key,
            Value = value,
            Environment = environment,
            UpdatedAt = DateTime.UtcNow,
            UpdatedBy = "admin-api"
        };
        _entries[MakeKey(environment, key)] = entry;
        return Task.FromResult(entry);
    }

    public Task<Dictionary<string, string>> GetConfigurationsByPrefixAsync(string prefix, CancellationToken cancellationToken = default) =>
        Task.FromResult(_entries.Values
            .Where(e => e.Key.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
            .GroupBy(e => e.Key, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(g => g.Key, g => g.First().Value));

    public Task DeleteConfigurationAsync(string key, CancellationToken cancellationToken = default)
    {
        foreach (var entryKey in _entries.Keys.Where(k => k.EndsWith($"::{key}", StringComparison.OrdinalIgnoreCase)).ToList())
        {
            _entries.TryRemove(entryKey, out _);
        }

        return Task.CompletedTask;
    }

    public Task ReloadConfigurationAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;

    public Task<List<ConfigurationDto>> GetConfigurationsAsync(string environment, CancellationToken cancellationToken = default) =>
        Task.FromResult(_entries.Values
            .Where(e => string.Equals(e.Environment, environment, StringComparison.OrdinalIgnoreCase))
            .OrderBy(e => e.Key, StringComparer.OrdinalIgnoreCase)
            .ToList());
}

/// <summary>POA stub: in-memory backup catalogue (mock data — no real backups are taken).</summary>
public sealed class InMemoryBackupService : IBackupService
{
    private readonly ConcurrentDictionary<string, BackupDto> _backups = new(StringComparer.OrdinalIgnoreCase);

    public InMemoryBackupService()
    {
        var seedId = "poa-seed-backup";
        _backups[seedId] = new BackupDto
        {
            Id = seedId,
            Type = nameof(BackupType.Full),
            Status = "Completed",
            SizeBytes = 42_000_000,
            Location = "memory://backups/poa-seed-backup",
            CreatedAt = DateTime.UtcNow.AddDays(-1),
            CompletedAt = DateTime.UtcNow.AddDays(-1).AddMinutes(7)
        };
    }

    public Task<BackupResult> CreateBackupAsync(BackupOptions options, CancellationToken cancellationToken = default)
    {
        var job = StartBackup(options);
        return Task.FromResult(new BackupResult
        {
            BackupId = job.Id,
            CreatedAt = job.StartedAt,
            SizeInBytes = 0,
            Location = $"memory://backups/{job.Id}",
            Success = true,
            Duration = TimeSpan.Zero
        });
    }

    public Task<RestoreResult> RestoreFromBackupAsync(string backupId, RestoreOptions options, CancellationToken cancellationToken = default) =>
        Task.FromResult(new RestoreResult
        {
            Success = _backups.ContainsKey(backupId),
            RestoredAt = DateTime.UtcNow,
            ErrorMessage = _backups.ContainsKey(backupId) ? null : $"Backup '{backupId}' not found",
            Duration = TimeSpan.Zero
        });

    public Task<BackupInfo[]> ListBackupsAsync(CancellationToken cancellationToken = default) =>
        Task.FromResult(_backups.Values.Select(b => new BackupInfo
        {
            BackupId = b.Id,
            CreatedAt = b.CreatedAt,
            SizeInBytes = b.SizeBytes,
            Type = b.Type,
            Description = "POA in-memory backup record (mock)",
            IsValid = true
        }).ToArray());

    public Task<bool> DeleteBackupAsync(string backupId, CancellationToken cancellationToken = default) =>
        Task.FromResult(_backups.TryRemove(backupId, out _));

    public Task<BackupValidationResult> ValidateBackupAsync(string backupId, CancellationToken cancellationToken = default) =>
        Task.FromResult(new BackupValidationResult
        {
            IsValid = _backups.ContainsKey(backupId),
            Issues = _backups.ContainsKey(backupId) ? Array.Empty<string>() : new[] { $"Backup '{backupId}' not found" },
            ValidatedAt = DateTime.UtcNow
        });

    public Task<string> ScheduleBackupAsync(BackupSchedule schedule, CancellationToken cancellationToken = default) =>
        Task.FromResult(Guid.NewGuid().ToString());

    public Task<Stream> ExportBackupAsync(string backupId, CancellationToken cancellationToken = default)
    {
        var payload = JsonSerializer.SerializeToUtf8Bytes(new { backupId, exportedAt = DateTime.UtcNow, mock = true });
        return Task.FromResult<Stream>(new MemoryStream(payload));
    }

    public Task<BackupHistory> GetBackupHistoryAsync(int limit = 100, CancellationToken cancellationToken = default)
    {
        var backups = _backups.Values.OrderByDescending(b => b.CreatedAt).Take(limit).ToList();
        return Task.FromResult(new BackupHistory
        {
            Backups = backups.Select(b => new BackupInfo
            {
                BackupId = b.Id,
                CreatedAt = b.CreatedAt,
                SizeInBytes = b.SizeBytes,
                Type = b.Type,
                IsValid = true
            }).ToArray(),
            TotalCount = backups.Count,
            LastBackupAt = backups.Count > 0 ? backups[0].CreatedAt : DateTime.MinValue
        });
    }

    public Task<BackupStatus> GetBackupStatusAsync(CancellationToken cancellationToken = default) =>
        Task.FromResult(new BackupStatus
        {
            IsBackupInProgress = _backups.Values.Any(b => b.Status == "InProgress"),
            LastSuccessfulBackup = _backups.Values
                .Where(b => b.Status == "Completed")
                .Select(b => (DateTime?)b.CreatedAt)
                .OrderByDescending(d => d)
                .FirstOrDefault()
        });

    public Task<BackupJob> StartBackupAsync(BackupOptions options, CancellationToken cancellationToken = default) =>
        Task.FromResult(StartBackup(options));

    public Task<RestoreJob> StartRestoreAsync(string backupId, RestoreOptions? options, CancellationToken cancellationToken = default)
    {
        if (!_backups.ContainsKey(backupId))
        {
            throw new InvalidOperationException($"Backup '{backupId}' not found");
        }

        return Task.FromResult(new RestoreJob
        {
            Id = Guid.NewGuid().ToString(),
            Status = "InProgress",
            StartedAt = DateTime.UtcNow
        });
    }

    public Task<List<BackupDto>> GetBackupHistoryAsync(CancellationToken cancellationToken = default) =>
        Task.FromResult(_backups.Values.OrderByDescending(b => b.CreatedAt).ToList());

    public Task<BackupStatusDto?> GetBackupStatusAsync(string id, CancellationToken cancellationToken = default)
    {
        if (!_backups.TryGetValue(id, out var backup))
        {
            return Task.FromResult<BackupStatusDto?>(null);
        }

        return Task.FromResult<BackupStatusDto?>(new BackupStatusDto
        {
            Id = backup.Id,
            Status = backup.Status,
            PercentComplete = backup.Status == "Completed" ? 100 : 25,
            CurrentOperation = backup.Status == "Completed" ? null : "Copying data (mock)",
            StartedAt = backup.CreatedAt,
            EstimatedCompletion = backup.CompletedAt
        });
    }

    private BackupJob StartBackup(BackupOptions options)
    {
        var id = Guid.NewGuid().ToString();
        // POA: the backup is recorded as immediately completed — no data is actually copied.
        _backups[id] = new BackupDto
        {
            Id = id,
            Type = options.Type.ToString(),
            Status = "Completed",
            SizeBytes = 0,
            Location = $"memory://backups/{id}",
            CreatedAt = DateTime.UtcNow,
            CompletedAt = DateTime.UtcNow
        };

        return new BackupJob
        {
            Id = id,
            Status = "Completed",
            Type = options.Type,
            StartedAt = DateTime.UtcNow
        };
    }
}

/// <summary>POA stub: in-memory reporting (generates small mock JSON reports).</summary>
public sealed class InMemoryReportingService : IReportingService
{
    private readonly ConcurrentDictionary<string, ReportResult> _reports = new(StringComparer.OrdinalIgnoreCase);
    private readonly ConcurrentDictionary<string, ScheduledReportDto> _schedules = new(StringComparer.OrdinalIgnoreCase);

    public Task<ReportResult> GenerateReportAsync(ReportRequest request, CancellationToken cancellationToken = default)
    {
        var content = JsonSerializer.SerializeToUtf8Bytes(new
        {
            reportType = request.ReportType,
            request.StartDate,
            request.EndDate,
            generatedAt = DateTime.UtcNow,
            mock = true
        });

        var result = new ReportResult
        {
            ReportId = Guid.NewGuid().ToString(),
            ReportType = request.ReportType,
            GeneratedAt = DateTime.UtcNow,
            Content = content,
            ContentType = "application/json",
            SizeInBytes = content.Length
        };
        _reports[result.ReportId] = result;
        return Task.FromResult(result);
    }

    public Task<string> ScheduleReportAsync(ScheduledReportRequest request, CancellationToken cancellationToken = default)
    {
        var id = Guid.NewGuid().ToString();
        _schedules[id] = new ScheduledReportDto
        {
            Id = id,
            Type = Enum.TryParse<ReportType>(request.ReportType, ignoreCase: true, out var type) ? type : ReportType.Usage,
            Schedule = request.CronExpression,
            Recipients = request.Recipients,
            IsActive = request.Enabled
        };
        return Task.FromResult(id);
    }

    public Task<bool> CancelScheduledReportAsync(string scheduleId, CancellationToken cancellationToken = default) =>
        Task.FromResult(_schedules.TryRemove(scheduleId, out _));

    public Task<List<ReportHistoryItem>> GetReportHistoryAsync(int limit = 100, CancellationToken cancellationToken = default) =>
        Task.FromResult(_reports.Values
            .OrderByDescending(r => r.GeneratedAt)
            .Take(limit)
            .Select(r => new ReportHistoryItem
            {
                ReportId = r.ReportId,
                ReportType = r.ReportType,
                GeneratedAt = r.GeneratedAt,
                GeneratedBy = "admin-api",
                SizeInBytes = r.SizeInBytes,
                Status = "Completed"
            })
            .ToList());

    public Task<List<ReportTemplate>> GetReportTemplatesAsync(CancellationToken cancellationToken = default) =>
        Task.FromResult(Enum.GetNames<ReportType>()
            .Select(name => new ReportTemplate
            {
                TemplateId = name.ToLowerInvariant(),
                Name = $"{name} report",
                Description = $"POA mock template for {name} reports",
                Category = "System"
            })
            .ToList());

    public Task<Stream> ExportReportAsync(string reportId, ExportFormat format, CancellationToken cancellationToken = default)
    {
        if (!_reports.TryGetValue(reportId, out var report))
        {
            throw new InvalidOperationException($"Report '{reportId}' not found");
        }

        return Task.FromResult<Stream>(new MemoryStream(report.Content));
    }

    public Task<ReportResult?> GetReportAsync(string reportId, CancellationToken cancellationToken = default) =>
        Task.FromResult(_reports.TryGetValue(reportId, out var report) ? report : null);

    public Task<AnalyticsResult> GenerateAnalyticsAsync(AnalyticsRequest request, CancellationToken cancellationToken = default) =>
        Task.FromResult(new AnalyticsResult
        {
            AnalyticsId = Guid.NewGuid().ToString(),
            GeneratedAt = DateTime.UtcNow
        });

    public async Task<ReportDto> GenerateReportAsync(ReportType type, ReportParameters parameters, CancellationToken cancellationToken = default)
    {
        var result = await GenerateReportAsync(
            new ReportRequest
            {
                ReportType = type.ToString(),
                StartDate = parameters.StartDate ?? DateTime.UtcNow.AddDays(-30),
                EndDate = parameters.EndDate ?? DateTime.UtcNow
            },
            cancellationToken);

        return new ReportDto
        {
            Id = result.ReportId,
            Type = type,
            Format = parameters.Format,
            Content = result.Content,
            GeneratedAt = result.GeneratedAt,
            Metadata = new Dictionary<string, object> { ["mock"] = true }
        };
    }

    public Task<List<ScheduledReportDto>> GetScheduledReportsAsync(CancellationToken cancellationToken = default) =>
        Task.FromResult(_schedules.Values.OrderBy(s => s.Id, StringComparer.OrdinalIgnoreCase).ToList());
}

/// <summary>POA stub: in-memory admin user directory (mock data — no identity provider integration).</summary>
public sealed class InMemoryUserManagementService : IUserManagementService
{
    private readonly ConcurrentDictionary<Guid, SystemUserDto> _users = new();

    public InMemoryUserManagementService()
    {
        var seed = new SystemUserDto
        {
            Id = Guid.Parse("00000000-0000-0000-0000-0000000000ad"),
            Email = "admin@example.gov.au",
            FirstName = "POA",
            LastName = "Administrator",
            Roles = new List<string> { "Admin" },
            IsActive = true,
            CreatedAt = DateTime.UtcNow.AddMonths(-1)
        };
        _users[seed.Id] = seed;
    }

    public Task<SystemUserDto> CreateUserAsync(CreateSystemUserDto request, CancellationToken cancellationToken = default)
    {
        var user = new SystemUserDto
        {
            Id = Guid.NewGuid(),
            Email = request.Email,
            FirstName = request.FirstName,
            LastName = request.LastName,
            Roles = request.Roles,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };
        _users[user.Id] = user;
        return Task.FromResult(user);
    }

    public Task<SystemUserDto> UpdateUserAsync(Guid userId, UpdateSystemUserDto request, CancellationToken cancellationToken = default)
    {
        if (!_users.TryGetValue(userId, out var user))
        {
            throw new InvalidOperationException($"User '{userId}' not found");
        }

        user.FirstName = request.FirstName ?? user.FirstName;
        user.LastName = request.LastName ?? user.LastName;
        user.Email = request.Email ?? user.Email;
        user.IsActive = request.IsActive ?? user.IsActive;
        return Task.FromResult(user);
    }

    public Task<bool> DeleteUserAsync(Guid userId, CancellationToken cancellationToken = default) =>
        Task.FromResult(_users.TryRemove(userId, out _));

    public Task<SystemUserDto?> GetUserByIdAsync(Guid userId, CancellationToken cancellationToken = default) =>
        Task.FromResult(_users.TryGetValue(userId, out var user) ? user : null);

    public Task<SystemUserDto?> GetUserByEmailAsync(string email, CancellationToken cancellationToken = default) =>
        Task.FromResult(_users.Values.FirstOrDefault(u => string.Equals(u.Email, email, StringComparison.OrdinalIgnoreCase)));

    public Task<List<SystemUserDto>> GetAllUsersAsync(CancellationToken cancellationToken = default) =>
        Task.FromResult(_users.Values.OrderBy(u => u.Email, StringComparer.OrdinalIgnoreCase).ToList());

    public Task<bool> AssignRoleAsync(Guid userId, string roleName, CancellationToken cancellationToken = default)
    {
        if (!_users.TryGetValue(userId, out var user))
        {
            return Task.FromResult(false);
        }

        if (!user.Roles.Contains(roleName, StringComparer.OrdinalIgnoreCase))
        {
            user.Roles.Add(roleName);
        }

        return Task.FromResult(true);
    }

    public Task<bool> RemoveRoleAsync(Guid userId, string roleName, CancellationToken cancellationToken = default)
    {
        if (!_users.TryGetValue(userId, out var user))
        {
            return Task.FromResult(false);
        }

        user.Roles.RemoveAll(r => string.Equals(r, roleName, StringComparison.OrdinalIgnoreCase));
        return Task.FromResult(true);
    }

    public Task<bool> ResetPasswordAsync(Guid userId, CancellationToken cancellationToken = default) =>
        Task.FromResult(_users.ContainsKey(userId));

    public Task<ResetPasswordResult> ResetPasswordAsync(string userId, CancellationToken cancellationToken = default)
    {
        var found = Guid.TryParse(userId, out var id) && _users.ContainsKey(id);
        return Task.FromResult(new ResetPasswordResult
        {
            Success = found,
            TemporaryPassword = found ? GenerateTemporaryPassword() : string.Empty,
            ExpiresAt = DateTime.UtcNow.AddHours(24)
        });
    }

    public async Task<AdminUserDto> CreateAdminUserAsync(CreateUserCommand command, CancellationToken cancellationToken = default)
    {
        var user = await CreateUserAsync(
            new CreateSystemUserDto
            {
                Email = command.Email,
                FirstName = command.FirstName,
                LastName = command.LastName,
                Roles = command.Roles
            },
            cancellationToken);

        return ToAdminUser(user);
    }

    public Task<AdminUserDto> UpdateAdminUserAsync(string id, UpdateUserCommand command, CancellationToken cancellationToken = default)
    {
        if (!Guid.TryParse(id, out var userId) || !_users.TryGetValue(userId, out var user))
        {
            // The GraphQL resolver translates null into a "not found" error.
            return Task.FromResult<AdminUserDto>(null!);
        }

        user.FirstName = command.FirstName ?? user.FirstName;
        user.LastName = command.LastName ?? user.LastName;
        user.Roles = command.Roles ?? user.Roles;
        user.IsActive = command.IsActive ?? user.IsActive;
        return Task.FromResult(ToAdminUser(user));
    }

    public Task<bool> LockUserAsync(Guid userId, string reason, CancellationToken cancellationToken = default)
    {
        if (!_users.TryGetValue(userId, out var user))
        {
            return Task.FromResult(false);
        }

        user.IsLocked = true;
        return Task.FromResult(true);
    }

    public Task<bool> UnlockUserAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        if (!_users.TryGetValue(userId, out var user))
        {
            return Task.FromResult(false);
        }

        user.IsLocked = false;
        return Task.FromResult(true);
    }

    private static AdminUserDto ToAdminUser(SystemUserDto user) => new()
    {
        Id = user.Id.ToString(),
        Email = user.Email,
        FirstName = user.FirstName,
        LastName = user.LastName,
        Roles = user.Roles,
        IsActive = user.IsActive,
        CreatedAt = user.CreatedAt,
        LastLoginAt = user.LastLoginAt
    };

    private static string GenerateTemporaryPassword()
    {
        // 18 random bytes -> 24 base64 chars; sufficient entropy for a temporary credential.
        Span<byte> bytes = stackalloc byte[18];
        RandomNumberGenerator.Fill(bytes);
        return Convert.ToBase64String(bytes);
    }
}

/// <summary>POA stub: in-memory key management (mock data — no Key Vault integration).</summary>
public sealed class InMemoryKeyManagementService : IKeyManagementService
{
    private readonly ConcurrentDictionary<string, KeyInfo> _keys = new(StringComparer.OrdinalIgnoreCase);

    public InMemoryKeyManagementService()
    {
        Seed("poa-encryption-key", "Encryption");
        Seed("poa-signing-key", "Signing");
    }

    private void Seed(string keyId, string keyType) =>
        _keys[keyId] = new KeyInfo
        {
            KeyId = keyId,
            KeyName = keyId,
            KeyType = keyType,
            KeySize = 2048,
            CreatedAt = DateTime.UtcNow.AddMonths(-1),
            Status = "Active"
        };

    public Task<KeyInfo> GenerateKeyAsync(GenerateKeyRequest request, CancellationToken cancellationToken = default)
    {
        var key = new KeyInfo
        {
            KeyId = Guid.NewGuid().ToString(),
            KeyName = request.KeyName,
            KeyType = request.KeyType,
            KeySize = request.KeySize,
            CreatedAt = DateTime.UtcNow,
            ExpiresAt = request.ExpiresAt,
            Status = "Active",
            Tags = request.Tags
        };
        _keys[key.KeyId] = key;
        return Task.FromResult(key);
    }

    public Task<KeyRotationResult> RotateKeyAsync(string keyId, CancellationToken cancellationToken = default)
    {
        if (!_keys.TryGetValue(keyId, out var existing))
        {
            return Task.FromResult(new KeyRotationResult
            {
                OldKeyId = keyId,
                Success = false,
                ErrorMessage = $"Key '{keyId}' not found"
            });
        }

        var newKey = new KeyInfo
        {
            KeyId = Guid.NewGuid().ToString(),
            KeyName = existing.KeyName,
            KeyType = existing.KeyType,
            KeySize = existing.KeySize,
            CreatedAt = DateTime.UtcNow,
            LastRotatedAt = DateTime.UtcNow,
            Status = "Active"
        };
        _keys[newKey.KeyId] = newKey;
        existing.Status = "Rotated";

        return Task.FromResult(new KeyRotationResult
        {
            OldKeyId = keyId,
            NewKeyId = newKey.KeyId,
            RotatedAt = DateTime.UtcNow,
            Success = true
        });
    }

    public Task<List<KeyInfo>> ListKeysAsync(CancellationToken cancellationToken = default) =>
        Task.FromResult(_keys.Values.OrderBy(k => k.KeyName, StringComparer.OrdinalIgnoreCase).ToList());

    public Task<KeyInfo?> GetKeyAsync(string keyId, CancellationToken cancellationToken = default) =>
        Task.FromResult(_keys.TryGetValue(keyId, out var key) ? key : null);

    public Task<bool> DeleteKeyAsync(string keyId, CancellationToken cancellationToken = default) =>
        Task.FromResult(_keys.TryRemove(keyId, out _));

    public Task<KeyExportData> ExportKeyAsync(string keyId, CancellationToken cancellationToken = default) =>
        throw new NotSupportedException("Key export is not supported by the POA in-memory key management stub.");

    public Task<KeyInfo> ImportKeyAsync(KeyImportData data, CancellationToken cancellationToken = default)
    {
        var key = new KeyInfo
        {
            KeyId = Guid.NewGuid().ToString(),
            KeyName = data.KeyName,
            KeyType = data.Format,
            CreatedAt = DateTime.UtcNow,
            Status = "Active",
            Tags = data.Tags
        };
        _keys[key.KeyId] = key;
        return Task.FromResult(key);
    }

    public Task<KeyRotationSchedule> GetRotationScheduleAsync(string keyId, CancellationToken cancellationToken = default) =>
        Task.FromResult(new KeyRotationSchedule
        {
            KeyId = keyId,
            AutoRotateEnabled = false,
            RotationIntervalDays = 90
        });

    public Task<bool> UpdateRotationScheduleAsync(string keyId, KeyRotationSchedule schedule, CancellationToken cancellationToken = default) =>
        Task.FromResult(_keys.ContainsKey(keyId));

    public async Task<KeyRotationResultDto> RotateKeysAsync(KeyType keyType, CancellationToken cancellationToken = default)
    {
        var targets = _keys.Values
            .Where(k => k.Status == "Active"
                && (keyType == KeyType.All || string.Equals(k.KeyType, keyType.ToString(), StringComparison.OrdinalIgnoreCase)))
            .Select(k => k.KeyId)
            .ToList();

        var newKeyIds = new List<string>();
        foreach (var keyId in targets)
        {
            var result = await RotateKeyAsync(keyId, cancellationToken);
            if (result.Success)
            {
                newKeyIds.Add(result.NewKeyId);
            }
        }

        return new KeyRotationResultDto
        {
            Success = true,
            KeysRotated = newKeyIds.Count,
            NewKeyIds = newKeyIds,
            CompletedAt = DateTime.UtcNow
        };
    }
}

/// <summary>POA stub: in-memory maintenance operations (mock results — no real optimization runs).</summary>
public sealed class InMemoryMaintenanceService : IMaintenanceService
{
    private MaintenanceStatus _status = new() { IsInMaintenance = false };

    public Task<bool> EnableMaintenanceModeAsync(MaintenanceOptions options, CancellationToken cancellationToken = default)
    {
        _status = new MaintenanceStatus
        {
            IsInMaintenance = true,
            StartedAt = DateTime.UtcNow,
            EstimatedEndTime = options.EstimatedEndTime,
            Reason = options.Reason,
            Message = options.MaintenanceMessage
        };
        return Task.FromResult(true);
    }

    public Task<bool> DisableMaintenanceModeAsync(CancellationToken cancellationToken = default)
    {
        _status = new MaintenanceStatus { IsInMaintenance = false };
        return Task.FromResult(true);
    }

    public Task<MaintenanceStatus> GetMaintenanceStatusAsync(CancellationToken cancellationToken = default) =>
        Task.FromResult(_status);

    public Task<string> ScheduleMaintenanceAsync(MaintenanceWindow window, CancellationToken cancellationToken = default) =>
        Task.FromResult(Guid.NewGuid().ToString());

    public Task<bool> CancelScheduledMaintenanceAsync(string maintenanceId, CancellationToken cancellationToken = default) =>
        Task.FromResult(true);

    public Task<OptimizationResult> OptimizeDatabaseAsync(CancellationToken cancellationToken = default) =>
        Task.FromResult(MockOptimizationResult());

    public Task<CleanupResult> CleanupLogsAsync(int daysToKeep, CancellationToken cancellationToken = default) =>
        Task.FromResult(new CleanupResult { Success = true, CompletedAt = DateTime.UtcNow });

    public Task<CleanupResult> CleanupTempFilesAsync(CancellationToken cancellationToken = default) =>
        Task.FromResult(new CleanupResult { Success = true, CompletedAt = DateTime.UtcNow });

    public Task<HealthCheckResult> RunHealthChecksAsync(CancellationToken cancellationToken = default) =>
        Task.FromResult(new HealthCheckResult { IsHealthy = true, CheckedAt = DateTime.UtcNow });

    public Task<MaintenanceResult> RunDatabaseMaintenanceAsync(MaintenanceOptions options, CancellationToken cancellationToken = default) =>
        Task.FromResult(new MaintenanceResult { Success = true });

    public Task<OptimizationResult> RunDatabaseMaintenanceAsync(CancellationToken cancellationToken = default) =>
        Task.FromResult(MockOptimizationResult());

    private static OptimizationResult MockOptimizationResult() => new()
    {
        Success = true,
        CompletedAt = DateTime.UtcNow,
        Duration = TimeSpan.Zero,
        Messages = new[] { "POA stub: no real database maintenance was performed (mock result)." }
    };
}
