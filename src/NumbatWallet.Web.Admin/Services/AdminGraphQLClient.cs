using System.Text;
using System.Text.Json;

namespace NumbatWallet.Web.Admin.Services;

/// <summary>
/// Typed GraphQL client for the admin portal's primary data operations.
/// Talks to the Web.Api /graphql endpoint using the service-to-service API key
/// (configured via AdminApi:ApiKey / AdminApi:TenantId — never hardcoded here).
/// </summary>
public interface IAdminGraphQLClient
{
    Task<AdminDashboardData?> GetDashboardDataAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<TenantNode>> GetTenantsAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<AdminWalletNode>> GetWalletsAsync(string? search = null, int first = 100, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<CredentialNode>> GetCredentialsAsync(int first = 50, CancellationToken cancellationToken = default);
}

public sealed class AdminGraphQLClient : IAdminGraphQLClient
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private readonly HttpClient _httpClient;
    private readonly ILogger<AdminGraphQLClient> _logger;

    public AdminGraphQLClient(HttpClient httpClient, ILogger<AdminGraphQLClient> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    public async Task<AdminDashboardData?> GetDashboardDataAsync(CancellationToken cancellationToken = default)
    {
        const string query = """
            query AdminDashboard {
              dashboardStatistics {
                totalUsers
                activeWallets
                totalCredentials
                credentialsIssuedToday
                credentialsExpiringThisWeek
              }
              credentialStatistics {
                totalCredentials
                activeCredentials
                revokedCredentials
                expiredCredentials
                issuedToday
                issuedThisWeek
              }
              systemHealth {
                status
                checkedAt
              }
              databaseStats {
                totalWallets
                totalCredentials
                totalPersons
                databaseSizeMB
              }
            }
            """;

        var data = await ExecuteAsync(query, variables: null, cancellationToken);
        if (data is null)
        {
            return null;
        }

        return data.Value.Deserialize<AdminDashboardData>(JsonOptions);
    }

    public async Task<IReadOnlyList<TenantNode>> GetTenantsAsync(CancellationToken cancellationToken = default)
    {
        // SECURITY: the backend TenantDto exposes connectionString — deliberately NOT selected here.
        const string query = """
            query AdminTenants {
              tenants(first: 50) {
                nodes {
                  id
                  name
                  identifier
                  description
                  isActive
                  createdAt
                }
              }
            }
            """;

        var data = await ExecuteAsync(query, variables: null, cancellationToken);
        return DeserializeList<TenantNode>(data, "tenants", "nodes");
    }

    public async Task<IReadOnlyList<AdminWalletNode>> GetWalletsAsync(string? search = null, int first = 100, CancellationToken cancellationToken = default)
    {
        const string query = """
            query AdminWallets($search: String, $first: Int) {
              adminWallets(search: $search, first: $first) {
                id
                personId
                personName
                name
                status
                isActive
                isSuspended
                createdAt
                credentialCount
              }
            }
            """;

        var data = await ExecuteAsync(query, new { search, first }, cancellationToken);
        return DeserializeList<AdminWalletNode>(data, "adminWallets");
    }

    public async Task<IReadOnlyList<CredentialNode>> GetCredentialsAsync(int first = 50, CancellationToken cancellationToken = default)
    {
        const string query = """
            query AdminCredentials($first: Int) {
              searchCredentials(first: $first) {
                nodes {
                  id
                  holderId
                  issuerId
                  type
                  status
                  issuanceDate
                  expirationDate
                  isRevoked
                }
              }
            }
            """;

        var data = await ExecuteAsync(query, new { first }, cancellationToken);
        return DeserializeList<CredentialNode>(data, "searchCredentials", "nodes");
    }

    private async Task<JsonElement?> ExecuteAsync(string query, object? variables, CancellationToken cancellationToken)
    {
        try
        {
            var payload = JsonSerializer.Serialize(new { query, variables }, JsonOptions);
            using var content = new StringContent(payload, Encoding.UTF8, "application/json");

            var response = await _httpClient.PostAsync("/graphql", content, cancellationToken);
            var body = await response.Content.ReadAsStringAsync(cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError("GraphQL request failed with status {StatusCode}: {Body}", (int)response.StatusCode, body);
                return null;
            }

            using var document = JsonDocument.Parse(body);

            if (document.RootElement.TryGetProperty("errors", out var errors))
            {
                _logger.LogError("GraphQL request returned errors: {Errors}", errors.GetRawText());
            }

            if (document.RootElement.TryGetProperty("data", out var data) && data.ValueKind == JsonValueKind.Object)
            {
                return data.Clone();
            }

            return null;
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException or JsonException)
        {
            _logger.LogError(ex, "GraphQL request to {BaseAddress} failed", _httpClient.BaseAddress);
            return null;
        }
    }

    private static IReadOnlyList<T> DeserializeList<T>(JsonElement? data, params string[] path)
    {
        if (data is null)
        {
            return Array.Empty<T>();
        }

        var current = data.Value;
        foreach (var segment in path)
        {
            if (current.ValueKind != JsonValueKind.Object || !current.TryGetProperty(segment, out current))
            {
                return Array.Empty<T>();
            }
        }

        if (current.ValueKind != JsonValueKind.Array)
        {
            return Array.Empty<T>();
        }

        return current.Deserialize<List<T>>(JsonOptions) ?? new List<T>();
    }
}

// --- Typed GraphQL response models (camelCase JSON handled by JsonSerializerDefaults.Web) ---

public sealed record AdminDashboardData(
    DashboardStatisticsNode? DashboardStatistics,
    CredentialStatisticsNode? CredentialStatistics,
    SystemHealthNode? SystemHealth,
    DatabaseStatsNode? DatabaseStats);

public sealed record DashboardStatisticsNode(
    int TotalUsers,
    int ActiveWallets,
    int TotalCredentials,
    int CredentialsIssuedToday,
    int CredentialsExpiringThisWeek);

public sealed record CredentialStatisticsNode(
    int TotalCredentials,
    int ActiveCredentials,
    int RevokedCredentials,
    int ExpiredCredentials,
    int IssuedToday,
    int IssuedThisWeek);

public sealed record SystemHealthNode(string Status, DateTime CheckedAt);

public sealed record DatabaseStatsNode(
    int TotalWallets,
    int TotalCredentials,
    int TotalPersons,
    decimal DatabaseSizeMB);

public sealed record TenantNode(
    string Id,
    string Name,
    string Identifier,
    string? Description,
    bool IsActive,
    DateTime CreatedAt);

public sealed record AdminWalletNode(
    string Id,
    string PersonId,
    string PersonName,
    string Name,
    string Status,
    bool IsActive,
    bool IsSuspended,
    DateTimeOffset CreatedAt,
    int CredentialCount);

public sealed record CredentialNode(
    string Id,
    string HolderId,
    string IssuerId,
    string Type,
    string Status,
    DateTime IssuanceDate,
    DateTime? ExpirationDate,
    bool IsRevoked);
