namespace NumbatWallet.Domain.Events;

public sealed record WalletSuspendedEvent(
    Guid WalletId,
    string Reason,
    DateTimeOffset SuspendedAt
) : DomainEventBase;