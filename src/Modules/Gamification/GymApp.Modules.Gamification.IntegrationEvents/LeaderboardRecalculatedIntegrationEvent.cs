using GymApp.Common.Domain;

namespace GymApp.Modules.Gamification.IntegrationEvents;

public sealed record LeaderboardRecalculatedIntegrationEvent(
    Guid GymId,
    string LeaderboardType) : IIntegrationEvent;
