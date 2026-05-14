using GymApp.Common.Domain;

namespace GymApp.Modules.Gyms.IntegrationEvents;

public sealed record StaffAddedToGymIntegrationEvent(
    Guid GymId,
    Guid UserId,
    string Role) : IDomainEvent;
