using GymApp.Common.Domain;

namespace GymApp.Modules.Training.IntegrationEvents;

public sealed record WorkoutLoggedIntegrationEvent(
    Guid UserId,
    Guid GymId,
    DateTime LoggedAt) : IDomainEvent;
