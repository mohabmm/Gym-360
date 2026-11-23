using GymApp.Common.Application.FeatureFlags;
using GymApp.Common.Application.Messaging;
using GymApp.Common.Domain;
using GymApp.Modules.Training.Application.Abstractions;
using GymApp.Modules.Training.Application.TrainingPlans.Challenge;
using GymApp.Modules.Training.Domain.Entities;
using GymApp.Modules.Training.Domain.Errors;
using GymApp.Modules.Training.Domain.Repositories;

namespace GymApp.Modules.Training.Application.TrainingPlans.Coaching;

internal sealed class PlanSettingsHandler
    : IQueryHandler<GetPlanSettingsQuery, PlanSettingsDto>,
      ICommandHandler<UpdatePlanSettingsCommand, PlanSettingsDto>
{
    private readonly PlanAccess _access;
    private readonly ChallengeAccess _challenge;
    private readonly ITrainingPlanSettingsRepository _settings;
    private readonly ITrainingUnitOfWork _unitOfWork;

    public PlanSettingsHandler(
        PlanAccess access, ChallengeAccess challenge, ITrainingPlanSettingsRepository settings, ITrainingUnitOfWork unitOfWork)
    {
        _access = access;
        _challenge = challenge;
        _settings = settings;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<PlanSettingsDto>> Handle(GetPlanSettingsQuery request, CancellationToken cancellationToken)
    {
        var enabled = await _access.EnsureEnabledAsync(request.Actor.GymId, cancellationToken);
        if (enabled.IsFailure)
            return Result.Failure<PlanSettingsDto>(enabled.Error);
        var saved = await _settings.GetAsync(request.Actor.GymId, cancellationToken);
        return Result.Success(await ToDtoAsync(saved ?? TrainingPlanSettings.ForGym(request.Actor.GymId), cancellationToken));
    }

    public async Task<Result<PlanSettingsDto>> Handle(UpdatePlanSettingsCommand request, CancellationToken cancellationToken)
    {
        var allowed = await EnsureCanUpdateAsync(request, cancellationToken);
        if (allowed.IsFailure)
            return Result.Failure<PlanSettingsDto>(allowed.Error);

        var settings = await _settings.GetAsync(request.Actor.GymId, cancellationToken);
        if (settings is null)
        {
            settings = TrainingPlanSettings.ForGym(request.Actor.GymId);
            await _settings.AddAsync(settings, cancellationToken);
        }
        var changed = Apply(settings, request);
        if (changed.IsFailure)
            return Result.Failure<PlanSettingsDto>(changed.Error);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return Result.Success(await ToDtoAsync(settings, cancellationToken));
    }

    private async Task<Result> EnsureCanUpdateAsync(UpdatePlanSettingsCommand request, CancellationToken cancellationToken)
    {
        var enabled = request.Challenge is null
            ? await _access.EnsureEnabledAsync(request.Actor.GymId, cancellationToken)
            : await _challenge.EnsureEnabledAsync(request.Actor.GymId, cancellationToken);
        if (enabled.IsFailure)
            return enabled;
        return request.Actor.IsGymManager ? Result.Success() : Result.Failure(TrainingPlanErrors.SettingsForbidden);
    }

    private static Result Apply(TrainingPlanSettings settings, UpdatePlanSettingsCommand request)
    {
        if (request.InactiveDays is { } days && settings.SetInactiveDays(days, request.Actor.UserId) is { IsFailure: true } badDays)
            return badDays;
        return request.Challenge is { } challenge ? settings.SetChallenge(challenge, request.Actor.UserId) : Result.Success();
    }

    private async Task<PlanSettingsDto> ToDtoAsync(TrainingPlanSettings s, CancellationToken cancellationToken) =>
        new(s.InactiveDays, TrainingPlanSettings.MinInactiveDays, TrainingPlanSettings.MaxInactiveDays)
        {
            Challenge = await _challenge.IsChallengeOnAsync(s.GymId, cancellationToken) ? ChallengeOf(s) : null
        };

    private static ChallengeSettingsDto ChallengeOf(TrainingPlanSettings s) => new()
    {
        Target = s.ChallengeTarget,
        WindowDays = s.ChallengeWindowDays,
        RewardTextAr = s.EffectiveRewardTextAr,
        RewardTextEn = s.EffectiveRewardTextEn,
        IsDefaultReward = s.RewardTextAr is null && s.RewardTextEn is null
    };
}
