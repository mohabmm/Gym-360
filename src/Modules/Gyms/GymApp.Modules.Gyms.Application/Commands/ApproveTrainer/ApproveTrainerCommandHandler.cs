using GymApp.Common.Application.Messaging;
using GymApp.Common.Domain;
using GymApp.Modules.Gyms.Application.Abstractions;
using GymApp.Modules.Gyms.Domain.Entities;
using GymApp.Modules.Gyms.Domain.Errors;
using GymApp.Modules.Gyms.Domain.Repositories;

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

        if (trainerRequest is null)
        {
            var gymResult = gym.AddTrainer(request.TrainerId);
            if (gymResult.IsFailure && gymResult.Error != GymErrors.TrainerAlreadyAdded)
                return gymResult;

            var newRequest = GymTrainerRequest.CreateApproved(request.GymId, request.TrainerId);
            await _trainerRequestRepository.AddAsync(newRequest, cancellationToken);
        }
        else
        {
            if (trainerRequest.Status == Domain.Enums.TrainerRequestStatus.Rejected)
                return Result.Failure(GymErrors.TrainerAlreadyRejected);

            var approveResult = trainerRequest.Approve();
            if (approveResult.IsFailure)
                return approveResult;

            var gymResult = gym.AddTrainer(request.TrainerId);
            if (gymResult.IsFailure && gymResult.Error != GymErrors.TrainerAlreadyAdded)
                return gymResult;

            _trainerRequestRepository.Update(trainerRequest);
        }

        _gymRepository.Update(gym);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
