using GymApp.Common.Domain;
using GymApp.Common.Infrastructure.Messaging;
using GymApp.Common.Infrastructure.Outbox;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Newtonsoft.Json;

namespace GymApp.Common.Infrastructure.Interceptors;

/// <summary>
/// On SaveChanges, serializes two sources into the outbox within the command's transaction:
/// (1) domain events raised on tracked aggregates, and (2) integration events explicitly
/// enqueued via <see cref="IIntegrationEventPublisher"/> (held in the scoped
/// <see cref="IntegrationEventQueue"/>). Scoped — so it can share the request's queue instance.
/// </summary>
public sealed class ConvertDomainEventsToOutboxMessagesInterceptor : SaveChangesInterceptor
{
    private static readonly JsonSerializerSettings JsonSettings = new()
    {
        TypeNameHandling = TypeNameHandling.All
    };

    private readonly IntegrationEventQueue _integrationEvents;

    public ConvertDomainEventsToOutboxMessagesInterceptor(IntegrationEventQueue integrationEvents)
    {
        _integrationEvents = integrationEvents;
    }

    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData,
        InterceptionResult<int> result,
        CancellationToken cancellationToken = default)
    {
        var dbContext = eventData.Context;

        if (dbContext is null)
        {
            return base.SavingChangesAsync(eventData, result, cancellationToken);
        }

        // (1) Domain events raised on tracked aggregates.
        var outboxMessages = dbContext.ChangeTracker
            .Entries<Entity>()
            .Select(entry => entry.Entity)
            .SelectMany(entity =>
            {
                var domainEvents = entity.GetDomainEvents();
                entity.ClearDomainEvents();
                return domainEvents;
            })
            .Select(ToOutboxMessage)
            .ToList();

        // (2) Integration events enqueued during this command — drained so they commit in the
        // same transaction as the state change (closes the publish-after-commit crash window).
        if (_integrationEvents.HasEvents)
        {
            outboxMessages.AddRange(_integrationEvents.Drain().Select(ToOutboxMessage));
        }

        // Add outbox messages to the outbox context
        // This works because we share the same SQL Server connection
        dbContext.Set<OutboxMessage>().AddRange(outboxMessages);

        return base.SavingChangesAsync(eventData, result, cancellationToken);
    }

    private static OutboxMessage ToOutboxMessage(object @event) => new()
    {
        Id = Guid.NewGuid(),
        OccurredOnUtc = DateTime.UtcNow,
        Type = @event.GetType().Name,
        Content = JsonConvert.SerializeObject(@event, JsonSettings)
    };
}
