using GymApp.Common.Domain;

namespace GymApp.Modules.Progress.IntegrationEvents;

public sealed record StreakMilestoneReachedIntegrationEvent(
    Guid UserId,
    Guid GymId,
    string StreakType,
    int MilestoneDay) : IDomainEvent;
