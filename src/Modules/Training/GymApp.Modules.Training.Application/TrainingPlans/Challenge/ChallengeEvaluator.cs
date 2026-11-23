using GymApp.Common.Application.FeatureFlags;
using GymApp.Common.Application.Messaging;
using GymApp.Modules.Training.Domain.Entities;
using GymApp.Modules.Training.Domain.Repositories;
using GymApp.Modules.Training.IntegrationEvents;
using Microsoft.Extensions.Logging;

namespace GymApp.Modules.Training.Application.TrainingPlans.Challenge;

/// <summary>
/// mem.first_month_challenge: after a plan workout is recorded (app or PT session), gives the
/// member their award the moment they reach the target, and announces it once. Safe to run
/// again: a member who already has an award is skipped, and the unique index settles a race.
/// </summary>
internal sealed class ChallengeEvaluator
{
    private readonly IFeatureFlagService _featureFlags;
    private readonly IChallengeAwardRepository _awards;
    private readonly ChallengeProgressReader _progress;
    private readonly IIntegrationEventPublisher _integrationEvents;
    private readonly ILogger<ChallengeEvaluator> _logger;

    public ChallengeEvaluator(
        IFeatureFlagService featureFlags,
        IChallengeAwardRepository awards,
        ChallengeProgressReader progress,
        IIntegrationEventPublisher integrationEvents,
        ILogger<ChallengeEvaluator> logger)
    {
        _featureFlags = featureFlags;
        _awards = awards;
        _progress = progress;
        _integrationEvents = integrationEvents;
        _logger = logger;
    }

    public async Task EvaluateAsync(Guid gymId, Guid traineeId, CancellationToken cancellationToken)
    {
        if (!await _featureFlags.IsEnabledAsync(gymId, FeatureKeys.MemFirstMonthChallenge, cancellationToken))
            return;
        if (await _awards.GetForTraineeAsync(gymId, traineeId, cancellationToken) is not null)
            return;

        var progress = await _progress.ReadAsync(gymId, traineeId, cancellationToken);
        if (!progress.IsReached)
            return;

        var award = ChallengeAward.Reach(progress.ToReach(gymId, traineeId));
        _integrationEvents.Enqueue(new FirstMonthChallengeReachedIntegrationEvent(
            gymId, award.Id, traineeId, award.TrainerId, award.CountAtReach, award.Target, award.RewardTextAr, award.RewardTextEn));
        if (!await _awards.TryAddAsync(award, cancellationToken))
            _logger.LogInformation("First-month challenge: trainee {TraineeId} at gym {GymId} already has an award; not announced again", traineeId, gymId);
    }
}
