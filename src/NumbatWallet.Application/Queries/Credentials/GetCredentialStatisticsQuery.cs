using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using NumbatWallet.Application.CQRS.Interfaces;
using NumbatWallet.Application.DTOs;
using NumbatWallet.Domain.Repositories;
using NumbatWallet.Domain.Specifications;
using NumbatWallet.SharedKernel.Enums;
using NumbatWallet.SharedKernel.Specifications;

namespace NumbatWallet.Application.Queries.Credentials;

public enum GroupBy
{
    Daily,
    Weekly,
    Monthly,
    Yearly
}

public record GetCredentialStatisticsQuery : IQuery<CredentialStatisticsDto>
{
    public DateTimeOffset From { get; init; } = DateTimeOffset.UtcNow.AddMonths(-1);
    public DateTimeOffset To { get; init; } = DateTimeOffset.UtcNow;
    public GroupBy GroupBy { get; init; } = GroupBy.Daily;
}

public record CredentialStatisticsDto
{
    public required int TotalCredentials { get; init; }
    public required int ActiveCredentials { get; init; }
    public required int RevokedCredentials { get; init; }
    public required int ExpiredCredentials { get; init; }
    public required int SuspendedCredentials { get; init; }
    public required Dictionary<string, int> CredentialsByType { get; init; }
    public required Dictionary<string, int> CredentialsByIssuer { get; init; }
    public required List<TimeSeriesDataPoint> IssuanceTimeSeries { get; init; }
    public required List<TimeSeriesDataPoint> RevocationTimeSeries { get; init; }
    public required DateTimeOffset PeriodStart { get; init; }
    public required DateTimeOffset PeriodEnd { get; init; }
}

public record TimeSeriesDataPoint
{
    public required DateTimeOffset Date { get; init; }
    public required int Count { get; init; }
    public required string Label { get; init; }
}

public class GetCredentialStatisticsQueryHandler : IQueryHandler<GetCredentialStatisticsQuery, CredentialStatisticsDto>
{
    private readonly ICredentialRepository _credentialRepository;
    private readonly IIssuerRepository _issuerRepository;
    private readonly ILogger<GetCredentialStatisticsQueryHandler> _logger;

    public GetCredentialStatisticsQueryHandler(
        ICredentialRepository credentialRepository,
        IIssuerRepository issuerRepository,
        ILogger<GetCredentialStatisticsQueryHandler> logger)
    {
        _credentialRepository = credentialRepository;
        _issuerRepository = issuerRepository;
        _logger = logger;
    }

    public async Task<CredentialStatisticsDto> HandleAsync(
        GetCredentialStatisticsQuery query,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation(
            "Generating credential statistics from {From} to {To} grouped by {GroupBy}",
            query.From, query.To, query.GroupBy);

        // Get all credentials in the period
        var periodSpec = new CredentialsIssuedInPeriodSpecification(query.From, query.To);
        var credentials = await _credentialRepository.FindAsync(periodSpec, cancellationToken);

        // Calculate basic statistics
        var totalCredentials = credentials.Count();
        var activeCredentials = credentials.Count(c => c.Status == CredentialStatus.Active && !IsExpired(c));
        var revokedCredentials = credentials.Count(c => c.Status == CredentialStatus.Revoked);
        var expiredCredentials = credentials.Count(c => IsExpired(c));
        var suspendedCredentials = credentials.Count(c => c.Status == CredentialStatus.Suspended);

        // Group by type
        var credentialsByType = credentials
            .GroupBy(c => c.CredentialType)
            .ToDictionary(g => g.Key, g => g.Count());

        // Group by issuer
        var credentialsByIssuer = await GetCredentialsByIssuer(credentials, cancellationToken);

        // Generate time series data
        var issuanceTimeSeries = GenerateTimeSeries(
            credentials,
            c => c.IssuedAt,
            query.GroupBy,
            query.From,
            query.To,
            "Issued");

        var revocationTimeSeries = GenerateTimeSeries(
            credentials.Where(c => c.RevokedAt.HasValue),
            c => c.RevokedAt!.Value,
            query.GroupBy,
            query.From,
            query.To,
            "Revoked");

        return new CredentialStatisticsDto
        {
            TotalCredentials = totalCredentials,
            ActiveCredentials = activeCredentials,
            RevokedCredentials = revokedCredentials,
            ExpiredCredentials = expiredCredentials,
            SuspendedCredentials = suspendedCredentials,
            CredentialsByType = credentialsByType,
            CredentialsByIssuer = credentialsByIssuer,
            IssuanceTimeSeries = issuanceTimeSeries,
            RevocationTimeSeries = revocationTimeSeries,
            PeriodStart = query.From,
            PeriodEnd = query.To
        };
    }

    private static bool IsExpired(Domain.Aggregates.Credential credential)
    {
        return credential.ExpiresAt.HasValue && credential.ExpiresAt.Value <= DateTimeOffset.UtcNow;
    }

    private async Task<Dictionary<string, int>> GetCredentialsByIssuer(
        IEnumerable<Domain.Aggregates.Credential> credentials,
        CancellationToken cancellationToken)
    {
        var groupedByIssuer = credentials.GroupBy(c => c.IssuerId);
        var result = new Dictionary<string, int>();

        foreach (var group in groupedByIssuer)
        {
            var issuer = await _issuerRepository.GetByIdAsync(group.Key, cancellationToken);
            var issuerName = issuer?.Name ?? "Unknown Issuer";
            result[issuerName] = group.Count();
        }

        return result;
    }

    private static List<TimeSeriesDataPoint> GenerateTimeSeries(
        IEnumerable<Domain.Aggregates.Credential> credentials,
        Func<Domain.Aggregates.Credential, DateTimeOffset> dateSelector,
        GroupBy groupBy,
        DateTimeOffset from,
        DateTimeOffset to,
        string labelPrefix)
    {
        var points = new List<TimeSeriesDataPoint>();
        var current = from;

        while (current <= to)
        {
            DateTimeOffset periodEnd;
            string label;

            switch (groupBy)
            {
                case GroupBy.Daily:
                    periodEnd = current.AddDays(1);
                    label = current.ToString("MMM dd");
                    break;
                case GroupBy.Weekly:
                    periodEnd = current.AddDays(7);
                    label = $"Week {current:MMM dd}";
                    break;
                case GroupBy.Monthly:
                    periodEnd = current.AddMonths(1);
                    label = current.ToString("MMM yyyy");
                    break;
                case GroupBy.Yearly:
                    periodEnd = current.AddYears(1);
                    label = current.ToString("yyyy");
                    break;
                default:
                    periodEnd = current.AddDays(1);
                    label = current.ToString("MMM dd");
                    break;
            }

            var count = credentials.Count(c =>
            {
                var date = dateSelector(c);
                return date >= current && date < periodEnd;
            });

            points.Add(new TimeSeriesDataPoint
            {
                Date = current,
                Count = count,
                Label = $"{labelPrefix} - {label}"
            });

            current = periodEnd;
        }

        return points;
    }
}

public class CredentialsIssuedInPeriodSpecification : Specification<Domain.Aggregates.Credential>
{
    public CredentialsIssuedInPeriodSpecification(DateTimeOffset from, DateTimeOffset to)
    {
        AddCriteria(c => c.IssuedAt >= from && c.IssuedAt <= to);
        ApplyOrderByDescending(c => c.IssuedAt);
    }
}