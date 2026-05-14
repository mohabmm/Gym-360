using GymApp.Common.Application.Messaging;
using GymApp.Common.Domain;
using GymApp.Modules.Training.Domain.Errors;
using GymApp.Modules.Training.Domain.Repositories;
using GymApp.Modules.Training.Application.Abstractions;

namespace GymApp.Modules.Training.Application.Commands.DeactivateRemoteWorkout;

internal sealed class DeactivateRemoteWorkoutCommandHandler : ICommandHandler<DeactivateRemoteWorkoutCommand>
{
    private readonly IRemoteWorkoutConfigRepository _remoteWorkoutConfigRepository;
    private readonly ITrainingUnitOfWork _unitOfWork;

    public DeactivateRemoteWorkoutCommandHandler(
        IRemoteWorkoutConfigRepository remoteWorkoutConfigRepository,
        ITrainingUnitOfWork unitOfWork)
    {
        _remoteWorkoutConfigRepository = remoteWorkoutConfigRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result> Handle(DeactivateRemoteWorkoutCommand request, CancellationToken cancellationToken)
    {
        var config = await _remoteWorkoutConfigRepository.GetByTrainerAndClientAsync(
            request.TrainerId, request.ClientId, request.GymId, cancellationToken);

        if (config is null)
        {
            return Result.Failure(TrainingErrors.RemoteWorkoutNotActive);
        }

        var result = config.Deactivate();
        if (result.IsFailure)
        {
            return result;
        }

        _remoteWorkoutConfigRepository.Update(config);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
