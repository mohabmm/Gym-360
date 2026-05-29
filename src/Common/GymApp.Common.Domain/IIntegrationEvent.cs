using MediatR;

namespace GymApp.Common.Domain;

/// <summary>
/// Marker for cross-module integration events. Unlike <see cref="IDomainEvent"/> (raised on an
/// aggregate and captured automatically by the SaveChanges interceptor), an integration event is
/// enqueued explicitly via <c>IIntegrationEventPublisher.Enqueue</c> and written to the outbox in
/// the same transaction as the command's state change, then delivered by the outbox processor.
/// </summary>
public interface IIntegrationEvent : INotification { }
