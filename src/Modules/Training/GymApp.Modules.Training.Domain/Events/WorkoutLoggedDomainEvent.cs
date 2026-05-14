using GymApp.Common.Domain;

namespace GymApp.Modules.Training.Domain.Events;

public sealed record WorkoutLoggedDomainEvent(Guid WorkoutLogId, Guid UserId, Guid GymId) : IDomainEvent;
