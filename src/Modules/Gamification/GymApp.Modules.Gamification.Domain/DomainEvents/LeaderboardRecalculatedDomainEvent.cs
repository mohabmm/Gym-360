using GymApp.Common.Domain;

namespace GymApp.Modules.Gamification.Domain.DomainEvents;

public sealed record LeaderboardRecalculatedDomainEvent(
    Guid LeaderboardId,
    Guid GymId,
    string LeaderboardType) : IDomainEvent;
