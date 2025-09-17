namespace NumbatWallet.SharedKernel.Interfaces;

public interface IDomainEvent
{
    Guid EventId { get; }
    DateTimeOffset OccurredAt { get; }
}