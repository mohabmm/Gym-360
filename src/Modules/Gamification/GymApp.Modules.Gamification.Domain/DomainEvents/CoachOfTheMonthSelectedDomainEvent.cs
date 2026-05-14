using GymApp.Common.Domain;

namespace GymApp.Modules.Gamification.Domain.DomainEvents;

public sealed record CoachOfTheMonthSelectedDomainEvent(
    Guid TrainerId,
    Guid GymId,
    int Month,
    int Year) : IDomainEvent;
