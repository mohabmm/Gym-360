using GymApp.Common.Application.Messaging;
using GymApp.Common.Domain;
using GymApp.Modules.Training.Application.Abstractions;
using GymApp.Modules.Training.Domain.Entities;
using GymApp.Modules.Training.Domain.Errors;
using GymApp.Modules.Training.Domain.Repositories;

namespace GymApp.Modules.Training.Application.TrainingPlans.Coaching;

internal sealed class PlanSettingsHandler
    : IQueryHandler<GetPlanSettingsQuery, PlanSettingsDto>,
      ICommandHandler<UpdatePlanSettingsCommand, PlanSettingsDto>
{
    private readonly PlanAccess _access;
    private readonly ITrainingPlanSettingsRepository _settings;
    private readonly ITrainingUnitOfWork _unitOfWork;

    public PlanSettingsHandler(PlanAccess access, ITrainingPlanSettingsRepository settings, ITrainingUnitOfWork unitOfWork)
    {
        _access = access;
        _settings = settings;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<PlanSettingsDto>> Handle(GetPlanSettingsQuery request, CancellationToken cancellationToken)
    {
        var enabled = await _access.EnsureEnabledAsync(request.Actor.GymId, cancellationToken);
        if (enabled.IsFailure)
            return Result.Failure<PlanSettingsDto>(enabled.Error);
        var saved = await _settings.GetAsync(request.Actor.GymId, cancellationToken);
        return Result.Success(ToDto(saved ?? TrainingPlanSettings.ForGym(request.Actor.GymId)));
    }

    public async Task<Result<PlanSettingsDto>> Handle(UpdatePlanSettingsCommand request, CancellationToken cancellationToken)
    {
        var enabled = await _access.EnsureEnabledAsync(request.Actor.GymId, cancellationToken);
        if (enabled.IsFailure)
            return Result.Failure<PlanSettingsDto>(enabled.Error);
        if (!request.Actor.IsGymManager)
            return Result.Failure<PlanSettingsDto>(TrainingPlanErrors.SettingsForbidden);

        var settings = await _settings.GetAsync(request.Actor.GymId, cancellationToken);
        if (settings is null)
        {
            settings = TrainingPlanSettings.ForGym(request.Actor.GymId);
            await _settings.AddAsync(settings, cancellationToken);
        }
        var changed = settings.SetInactiveDays(request.InactiveDays, request.Actor.UserId);
        if (changed.IsFailure)
            return Result.Failure<PlanSettingsDto>(changed.Error);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return Result.Success(ToDto(settings));
    }

    internal static PlanSettingsDto ToDto(TrainingPlanSettings s) =>
        new(s.InactiveDays, TrainingPlanSettings.MinInactiveDays, TrainingPlanSettings.MaxInactiveDays);
}
