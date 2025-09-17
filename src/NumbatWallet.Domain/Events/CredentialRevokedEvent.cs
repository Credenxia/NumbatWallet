namespace NumbatWallet.Domain.Events;

public sealed record CredentialRevokedEvent(
    Guid CredentialId,
    Guid WalletId,
    string Reason,
    DateTimeOffset RevokedAt
) : DomainEventBase;