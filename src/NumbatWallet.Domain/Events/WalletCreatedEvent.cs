namespace NumbatWallet.Domain.Events;

public sealed record WalletCreatedEvent(
    Guid WalletId,
    Guid PersonId,
    Guid TenantId,
    string WalletDid
) : DomainEventBase;