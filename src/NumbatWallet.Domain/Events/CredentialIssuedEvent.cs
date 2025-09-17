namespace NumbatWallet.Domain.Events;

public sealed record CredentialIssuedEvent(
    Guid CredentialId,
    Guid WalletId,
    Guid IssuerId,
    string CredentialType,
    DateTimeOffset IssuedAt
) : DomainEventBase;