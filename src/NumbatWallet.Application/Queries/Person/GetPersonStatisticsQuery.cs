using NumbatWallet.Application.CQRS.Interfaces;
using NumbatWallet.Domain.Interfaces;
using NumbatWallet.SharedKernel.Enums;

namespace NumbatWallet.Application.Queries.Person;

public sealed class GetPersonStatisticsQuery : IQuery<PersonStatisticsDto>
{
}

public sealed class PersonStatisticsDto
{
    public int TotalPersons { get; init; }
    public int VerifiedPersons { get; init; }
    public int PendingVerificationPersons { get; init; }
    public int SuspendedPersons { get; init; }
    public Dictionary<string, int> StatusBreakdown { get; init; } = new();
    public DateTime GeneratedAt { get; init; } = DateTime.UtcNow;
}

public sealed class GetPersonStatisticsQueryHandler : IQueryHandler<GetPersonStatisticsQuery, PersonStatisticsDto>
{
    private readonly IPersonRepository _personRepository;

    public GetPersonStatisticsQueryHandler(IPersonRepository personRepository)
    {
        _personRepository = personRepository;
    }

    public async Task<PersonStatisticsDto> HandleAsync(GetPersonStatisticsQuery request, CancellationToken cancellationToken)
    {
        var statistics = await _personRepository.GetPersonStatisticsAsync(cancellationToken);

        var verifiedCount = await _personRepository.CountByStatusAsync(PersonStatus.Verified, cancellationToken);
        var pendingCount = await _personRepository.CountByStatusAsync(PersonStatus.PendingVerification, cancellationToken);
        var suspendedCount = await _personRepository.CountByStatusAsync(PersonStatus.Suspended, cancellationToken);

        return new PersonStatisticsDto
        {
            TotalPersons = statistics.GetValueOrDefault("Total", 0),
            VerifiedPersons = verifiedCount,
            PendingVerificationPersons = pendingCount,
            SuspendedPersons = suspendedCount,
            StatusBreakdown = new Dictionary<string, int>
            {
                ["Verified"] = verifiedCount,
                ["PendingVerification"] = pendingCount,
                ["Suspended"] = suspendedCount
            }
        };
    }
}