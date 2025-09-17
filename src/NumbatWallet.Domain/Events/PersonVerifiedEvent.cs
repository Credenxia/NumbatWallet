namespace NumbatWallet.Domain.Events;

public sealed record PersonVerifiedEvent(
    Guid PersonId,
    bool EmailVerified,
    bool PhoneVerified,
    DateTimeOffset VerifiedAt
) : DomainEventBase;