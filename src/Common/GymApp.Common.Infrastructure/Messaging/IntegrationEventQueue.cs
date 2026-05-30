using GymApp.Common.Domain;

namespace GymApp.Common.Infrastructure.Messaging;

/// <summary>
/// Scoped, per-request holder for integration events awaiting durable dispatch. Populated by
/// <see cref="IntegrationEventPublisher"/> during a command and drained by
/// <c>ConvertDomainEventsToOutboxMessagesInterceptor</c> on SaveChanges, so the events land in
/// the outbox within the same transaction as the command's state change.
/// </summary>
public sealed class IntegrationEventQueue
{
    private readonly List<IIntegrationEvent> _events = new();

    public void Add(IIntegrationEvent integrationEvent) => _events.Add(integrationEvent);

    public bool HasEvents => _events.Count > 0;

    /// <summary>Returns the queued events and clears the queue (so a second SaveChanges in the
    /// same scope does not re-emit them).</summary>
    public IReadOnlyList<IIntegrationEvent> Drain()
    {
        var drained = _events.ToList();
        _events.Clear();
        return drained;
    }
}
