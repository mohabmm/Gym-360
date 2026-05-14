using GymApp.Common.Domain;

namespace GymApp.Modules.Identity.IntegrationEvents;

public sealed record UserLinkedToGymIntegrationEvent(
    Guid UserId,
    Guid GymId,
    string Role) : IDomainEvent;
