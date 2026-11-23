using GymApp.Common.Domain;

namespace GymApp.Modules.Training.IntegrationEvents;

/// <summary>
/// mem.first_month_challenge: a new member reached the target. Raised once per member per gym
/// (the award row is unique), so the trainee, their coach and the desk each hear it once.
/// </summary>
public sealed record FirstMonthChallengeReachedIntegrationEvent(
    Guid GymId,
    Guid AwardId,
    Guid TraineeId,
    Guid? TrainerId,
    int Count,
    int Target,
    string RewardTextAr,
    string RewardTextEn) : IIntegrationEvent;
