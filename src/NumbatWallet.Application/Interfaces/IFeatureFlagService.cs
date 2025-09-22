using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using NumbatWallet.Application.DTOs;

namespace NumbatWallet.Application.Interfaces;

/// <summary>
/// Service for managing feature flags
/// </summary>
public interface IFeatureFlagService
{
    /// <summary>
    /// Check if a feature is enabled
    /// </summary>
    Task<bool> IsFeatureEnabledAsync(string featureName, CancellationToken cancellationToken = default);

    /// <summary>
    /// Enable a feature
    /// </summary>
    Task EnableFeatureAsync(string featureName, CancellationToken cancellationToken = default);

    /// <summary>
    /// Disable a feature
    /// </summary>
    Task DisableFeatureAsync(string featureName, CancellationToken cancellationToken = default);

    /// <summary>
    /// Toggle a feature state
    /// </summary>
    Task<bool> ToggleFeatureAsync(string featureName, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get all feature flags
    /// </summary>
    Task<Dictionary<string, bool>> GetAllFeaturesAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Check if a feature is enabled for a specific user
    /// </summary>
    Task<bool> IsFeatureEnabledForUserAsync(string featureName, string userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Toggle a feature flag and return its DTO
    /// </summary>
    Task<FeatureFlagDto> ToggleFlagAsync(string flagId, bool enabled, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get all feature flags as DTOs
    /// </summary>
    Task<List<FeatureFlagDto>> GetAllFlagsAsync(CancellationToken cancellationToken = default);
}