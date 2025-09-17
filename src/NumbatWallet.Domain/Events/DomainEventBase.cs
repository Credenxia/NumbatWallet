using NumbatWallet.SharedKernel.Interfaces;

namespace NumbatWallet.Domain.Events;

public abstract record DomainEventBase : IDomainEvent
{
    protected DomainEventBase()
    {
        EventId = Guid.NewGuid();
        OccurredAt = DateTimeOffset.UtcNow;
    }

    public Guid EventId { get; }
    public DateTimeOffset OccurredAt { get; }
}