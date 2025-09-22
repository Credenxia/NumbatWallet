using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using NumbatWallet.Application.DTOs;

namespace NumbatWallet.Application.Interfaces;

/// <summary>
/// Service for managing application configuration
/// </summary>
public interface IConfigurationService
{
    /// <summary>
    /// Get a configuration value
    /// </summary>
    Task<string?> GetConfigurationAsync(string key, CancellationToken cancellationToken = default);

    /// <summary>
    /// Set a configuration value
    /// </summary>
    Task SetConfigurationAsync(string key, string value, CancellationToken cancellationToken = default);

    /// <summary>
    /// Update a configuration value with environment support
    /// </summary>
    Task<ConfigurationDto> UpdateConfigurationAsync(string key, string value, string environment, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get all configuration values for a prefix
    /// </summary>
    Task<Dictionary<string, string>> GetConfigurationsByPrefixAsync(string prefix, CancellationToken cancellationToken = default);

    /// <summary>
    /// Delete a configuration value
    /// </summary>
    Task DeleteConfigurationAsync(string key, CancellationToken cancellationToken = default);

    /// <summary>
    /// Reload configuration from source
    /// </summary>
    Task ReloadConfigurationAsync(CancellationToken cancellationToken = default);
}