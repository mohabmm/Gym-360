using GymApp.Common.Application.Abstractions;
using GymApp.Common.Application.Errors;
using GymApp.Common.Application.Messaging;
using GymApp.Common.Domain;
using GymApp.Modules.Training.Domain.Entities;
using GymApp.Modules.Training.Domain.Repositories;
using GymApp.Modules.Training.Application.Abstractions;

namespace GymApp.Modules.Training.Application.Commands.ActivateRemoteWorkout;

internal sealed class ActivateRemoteWorkoutCommandHandler : ICommandHandler<ActivateRemoteWorkoutCommand>
{
    private readonly IRemoteWorkoutConfigRepository _remoteWorkoutConfigRepository;
    private readonly ITrainingUnitOfWork _unitOfWork;
    private readonly IGymPlanService _gymPlanService;

    public ActivateRemoteWorkoutCommandHandler(
        IRemoteWorkoutConfigRepository remoteWorkoutConfigRepository,
        ITrainingUnitOfWork unitOfWork,
        IGymPlanService gymPlanService)
    {
        _remoteWorkoutConfigRepository = remoteWorkoutConfigRepository;
        _unitOfWork = unitOfWork;
        _gymPlanService = gymPlanService;
    }

    public async Task<Result> Handle(ActivateRemoteWorkoutCommand request, CancellationToken cancellationToken)
    {
        var isEnabled = await _gymPlanService.IsFeatureEnabledAsync(
            request.GymId, "remote_workout_mode", cancellationToken);

        if (!isEnabled)
        {
            var currentPlan = await _gymPlanService.GetPlanCodeAsync(request.GymId, cancellationToken);
            return Result.Failure(PlanErrors.FeatureNotAvailable(
                "remote_workout_mode", currentPlan, "Pro"));
        }

        var existing = await _remoteWorkoutConfigRepository.GetByTrainerAndClientAsync(
            request.TrainerId, request.ClientId, request.GymId, cancellationToken);

        if (existing is not null)
        {
            var activateResult = existing.Activate(request.Reason);
            if (activateResult.IsFailure)
            {
                return activateResult;
            }

            _remoteWorkoutConfigRepository.Update(existing);
        }
        else
        {
            var config = RemoteWorkoutConfig.Create(
                request.TrainerId,
                request.ClientId,
                request.GymId,
                request.Reason);

            await _remoteWorkoutConfigRepository.AddAsync(config, cancellationToken);
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
