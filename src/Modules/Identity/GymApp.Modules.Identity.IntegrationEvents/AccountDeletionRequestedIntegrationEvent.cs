using GymApp.Common.Domain;

namespace GymApp.Modules.Identity.IntegrationEvents;

public sealed record AccountDeletionRequestedIntegrationEvent(
    Guid UserId,
    DateTime ScheduledDeletionUtc) : IDomainEvent;
