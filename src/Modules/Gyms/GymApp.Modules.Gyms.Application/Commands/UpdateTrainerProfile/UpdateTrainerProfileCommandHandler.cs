using GymApp.Common.Application.Messaging;
using GymApp.Common.Domain;
using GymApp.Modules.Gyms.Application.Abstractions;
using GymApp.Modules.Gyms.Domain.Entities;
using GymApp.Modules.Gyms.Domain.Errors;
using GymApp.Modules.Gyms.Domain.Repositories;
using GymApp.Modules.Gyms.Domain.ValueObjects;

namespace GymApp.Modules.Gyms.Application.Commands.UpdateTrainerProfile;

internal sealed class UpdateTrainerProfileCommandHandler : ICommandHandler<UpdateTrainerProfileCommand>
{
    private readonly IGymRepository _gymRepository;
    private readonly IGymTrainerRequestRepository _trainerRequestRepository;
    private readonly IGymsUnitOfWork _unitOfWork;

    public UpdateTrainerProfileCommandHandler(
        IGymRepository gymRepository,
        IGymTrainerRequestRepository trainerRequestRepository,
        IGymsUnitOfWork unitOfWork)
    {
        _gymRepository = gymRepository;
        _trainerRequestRepository = trainerRequestRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result> Handle(UpdateTrainerProfileCommand request, CancellationToken cancellationToken)
    {
        var gym = await _gymRepository.GetByIdAsync(request.GymId, cancellationToken);
        if (gym is null)
            return Result.Failure(GymErrors.NotFound);

        var isActive = gym.TrainerIds.Contains(new TrainerId(request.TrainerId));
        var existing = await _trainerRequestRepository.GetAsync(request.GymId, request.TrainerId, cancellationToken);

        if (!isActive && existing is null)
            return Result.Failure(GymErrors.TrainerNotFound);

        if (existing is null)
        {
            var created = GymTrainerRequest.CreateApproved(request.GymId, request.TrainerId);
            created.UpdateProfile(request.Specializations, request.Bio);
            await _trainerRequestRepository.AddAsync(created, cancellationToken);
        }
        else
        {
            existing.UpdateProfile(request.Specializations, request.Bio);
            _trainerRequestRepository.Update(existing);
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}
