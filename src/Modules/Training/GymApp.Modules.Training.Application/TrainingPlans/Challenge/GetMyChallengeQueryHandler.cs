using GymApp.Common.Application.Messaging;
using GymApp.Common.Domain;
using GymApp.Modules.Training.Application.TrainingPlans.Workouts;
using GymApp.Modules.Training.Domain.Entities;
using GymApp.Modules.Training.Domain.Repositories;

namespace GymApp.Modules.Training.Application.TrainingPlans.Challenge;

/// <summary>
/// "8 / 12" and the reward. Once reached, the award's own numbers and reward text are shown,
/// so a later settings change or a reversed session does not take the win away.
/// </summary>
internal sealed class GetMyChallengeQueryHandler : IQueryHandler<GetMyChallengeQuery, ChallengeStatusDto>
{
    private readonly ChallengeAccess _access;
    private readonly ChallengeProgressReader _progress;
    private readonly IChallengeAwardRepository _awards;

    public GetMyChallengeQueryHandler(ChallengeAccess access, ChallengeProgressReader progress, IChallengeAwardRepository awards)
    {
        _access = access;
        _progress = progress;
        _awards = awards;
    }

    public async Task<Result<ChallengeStatusDto>> Handle(GetMyChallengeQuery request, CancellationToken cancellationToken)
    {
        var enabled = await _access.EnsureEnabledAsync(request.Actor.GymId, cancellationToken);
        if (enabled.IsFailure)
            return Result.Failure<ChallengeStatusDto>(enabled.Error);

        var gymId = request.Actor.GymId;
        var traineeId = request.Actor.UserId;
        var progress = await _progress.ReadAsync(gymId, traineeId, cancellationToken);
        var award = await _awards.GetForTraineeAsync(gymId, traineeId, cancellationToken);
        var today = TrainingCalendar.Today();
        return Result.Success(award is null ? InProgress(progress, today) : Reached(award, progress.Count));
    }

    private static ChallengeStatusDto InProgress(ChallengeProgress progress, DateOnly today) => new()
    {
        Status = progress.StatusOn(today),
        Count = progress.Count,
        Target = progress.Settings.ChallengeTarget,
        WindowStart = progress.Window?.Start,
        WindowEnd = progress.Window?.End,
        DaysLeft = progress.Window?.DaysLeft(today) ?? 0,
        RewardTextAr = progress.Settings.EffectiveRewardTextAr,
        RewardTextEn = progress.Settings.EffectiveRewardTextEn
    };

    private static ChallengeStatusDto Reached(ChallengeAward award, int countNow) => new()
    {
        Status = ChallengeStates.Reached,
        Count = Math.Max(countNow, award.CountAtReach),
        Target = award.Target,
        WindowStart = award.WindowStart,
        WindowEnd = award.WindowEnd,
        RewardTextAr = award.RewardTextAr,
        RewardTextEn = award.RewardTextEn,
        ReachedAt = award.ReachedAt,
        DeliveredAt = award.DeliveredAt
    };
}
