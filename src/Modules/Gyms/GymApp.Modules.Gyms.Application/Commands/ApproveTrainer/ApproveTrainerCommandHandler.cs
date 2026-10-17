using GymApp.Common.Application.Messaging;
using GymApp.Common.Domain;
using GymApp.Modules.Gyms.Application.Abstractions;
using GymApp.Modules.Gyms.Domain.Entities;
using GymApp.Modules.Gyms.Domain.Errors;
using GymApp.Modules.Gyms.Domain.Repositories;
using GymApp.Modules.Gyms.Domain.ValueObjects;

namespace GymApp.Modules.Gyms.Application.Commands.ApproveTrainer;

internal sealed class ApproveTrainerCommandHandler : ICommandHandler<ApproveTrainerCommand>
{
    private readonly IGymRepository _gymRepository;
    private readonly IGymTrainerRequestRepository _trainerRequestRepository;
    private readonly IGymsUnitOfWork _unitOfWork;

    public ApproveTrainerCommandHandler(
        IGymRepository gymRepository,
        IGymTrainerRequestRepository trainerRequestRepository,
        IGymsUnitOfWork unitOfWork)
    {
        _gymRepository = gymRepository;
        _trainerRequestRepository = trainerRequestRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result> Handle(ApproveTrainerCommand request, CancellationToken cancellationToken)
    {
        var gym = await _gymRepository.GetByIdAsync(request.GymId, cancellationToken);
        if (gym is null)
            return Result.Failure(GymErrors.NotFound);

        var trainerRequest = await _trainerRequestRepository.GetAsync(
            request.GymId, request.TrainerId, cancellationToken);

        var result = trainerRequest is null
            ? await CreateApprovedRequestAsync(gym, request, cancellationToken)
            : ApproveExistingRequest(gym, trainerRequest, new TrainerId(request.TrainerId));
        if (result.IsFailure)
            return result;

        _gymRepository.Update(gym);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }

    private async Task<Result> CreateApprovedRequestAsync(Gym gym, ApproveTrainerCommand request, CancellationToken cancellationToken)
    {
        var gymResult = AddTrainerToRoster(gym, new TrainerId(request.TrainerId));
        if (gymResult.IsFailure)
            return gymResult;

        var newRequest = GymTrainerRequest.CreateApproved(request.GymId, request.TrainerId);
        await _trainerRequestRepository.AddAsync(newRequest, cancellationToken);
        return Result.Success();
    }

    private Result ApproveExistingRequest(Gym gym, GymTrainerRequest trainerRequest, TrainerId trainerId)
    {
        if (trainerRequest.Status == Domain.Enums.TrainerRequestStatus.Rejected)
            return Result.Failure(GymErrors.TrainerAlreadyRejected);

        var approveResult = trainerRequest.Approve();
        if (approveResult.IsFailure)
            return approveResult;

        var gymResult = AddTrainerToRoster(gym, trainerId);
        if (gymResult.IsFailure)
            return gymResult;

        _trainerRequestRepository.Update(trainerRequest);
        return Result.Success();
    }

    // A trainer already on the roster is fine: approval is idempotent for the gym side.
    private static Result AddTrainerToRoster(Gym gym, TrainerId trainerId)
    {
        var gymResult = gym.AddTrainer(trainerId);
        return gymResult.IsFailure && gymResult.Error != GymErrors.TrainerAlreadyAdded
            ? gymResult
            : Result.Success();
    }
}
