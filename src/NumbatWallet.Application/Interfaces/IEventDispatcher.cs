using NumbatWallet.SharedKernel.Interfaces;

namespace NumbatWallet.Application.Interfaces;

public interface IEventDispatcher
{
    Task DispatchAsync(IDomainEvent domainEvent, CancellationToken cancellationToken = default);
    Task DispatchAsync(IEnumerable<IDomainEvent> domainEvents, CancellationToken cancellationToken = default);
}

#pragma warning disable CA1711 // EventHandler naming is intentional for domain events
public interface IDomainEventHandler<in TEvent> where TEvent : IDomainEvent
{
    Task HandleAsync(TEvent domainEvent, CancellationToken cancellationToken = default);
}
#pragma warning restore CA1711