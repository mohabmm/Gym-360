using GymApp.Common.Application.Messaging;
using GymApp.Common.Domain;

namespace GymApp.Common.Infrastructure.Messaging;

/// <summary>
/// Default <see cref="IIntegrationEventPublisher"/>: enqueues onto the scoped
/// <see cref="IntegrationEventQueue"/> instead of publishing in-process. The SaveChanges
/// interceptor turns the queue into outbox rows; the outbox processor delivers them with retry.
/// </summary>
public sealed class IntegrationEventPublisher : IIntegrationEventPublisher
{
    private readonly IntegrationEventQueue _queue;

    public IntegrationEventPublisher(IntegrationEventQueue queue) => _queue = queue;

    public void Enqueue(IIntegrationEvent integrationEvent) => _queue.Add(integrationEvent);
}
