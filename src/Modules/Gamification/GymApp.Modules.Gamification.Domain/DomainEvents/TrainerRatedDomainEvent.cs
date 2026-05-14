using GymApp.Common.Domain;

namespace GymApp.Modules.Gamification.Domain.DomainEvents;

public sealed record TrainerRatedDomainEvent(
    Guid RatingId,
    Guid TrainerId,
    Guid GymId,
    int Stars) : IDomainEvent;
