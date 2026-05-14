using GymApp.Common.Domain;

namespace GymApp.Modules.Progress.Domain.DomainEvents;

public sealed record StreakMilestoneReachedDomainEvent(
    Guid UserId,
    Guid GymId,
    string StreakType,
    int MilestoneDay) : IDomainEvent;
