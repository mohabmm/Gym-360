using GymApp.Common.Application.Messaging;
using GymApp.Common.Domain;
using GymApp.Modules.Gyms.Application.Abstractions;
using GymApp.Modules.Gyms.Domain.Entities;
using GymApp.Modules.Gyms.Domain.Errors;
using GymApp.Modules.Gyms.Domain.Repositories;

namespace GymApp.Modules.Gyms.Application.Commands.RejectTrainer;

internal sealed class RejectTrainerCommandHandler : ICommandHandler<RejectTrainerCommand>
{
    private readonly IGymRepository _gymRepository;
    private readonly IGymTrainerRequestRepository _trainerRequestRepository;
    private readonly IGymsUnitOfWork _unitOfWork;

    public RejectTrainerCommandHandler(
        IGymRepository gymRepository,
        IGymTrainerRequestRepository trainerRequestRepository,
        IGymsUnitOfWork unitOfWork)
    {
        _gymRepository = gymRepository;
        _trainerRequestRepository = trainerRequestRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result> Handle(RejectTrainerCommand request, CancellationToken cancellationToken)
    {
        var gym = await _gymRepository.GetByIdAsync(request.GymId, cancellationToken);
        if (gym is null)
            return Result.Failure(GymErrors.NotFound);

        var trainerRequest = await _trainerRequestRepository.GetAsync(
            request.GymId, request.TrainerId, cancellationToken);

        if (trainerRequest is null)
        {
            gym.RemoveTrainer(request.TrainerId);
            var newRequest = GymTrainerRequest.CreateRejected(request.GymId, request.TrainerId);
            await _trainerRequestRepository.AddAsync(newRequest, cancellationToken);
        }
        else
        {
            trainerRequest.Reject();
            gym.RemoveTrainer(request.TrainerId);
            _trainerRequestRepository.Update(trainerRequest);
        }

        _gymRepository.Update(gym);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
