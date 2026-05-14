using GymApp.Common.Domain;

namespace GymApp.Modules.Scheduling.Domain.Events;

public sealed record ExerciseLoggedDomainEvent(
    Guid SessionId,
    Guid ExerciseLogId,
    Guid TrainerId,
    Guid MemberId) : IDomainEvent;
