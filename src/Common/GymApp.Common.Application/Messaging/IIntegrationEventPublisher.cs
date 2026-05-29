using GymApp.Common.Domain;

namespace GymApp.Common.Application.Messaging;

/// <summary>
/// Enqueues an integration event for durable, transactional delivery. The event is held in a
/// scoped queue and serialized into an outbox row by the SaveChanges interceptor, so it commits
/// atomically with the command's state change. It is NOT published in-process — the outbox
/// processor delivers it (with retry) on a later tick.
/// </summary>
public interface IIntegrationEventPublisher
{
    void Enqueue(IIntegrationEvent integrationEvent);
}
