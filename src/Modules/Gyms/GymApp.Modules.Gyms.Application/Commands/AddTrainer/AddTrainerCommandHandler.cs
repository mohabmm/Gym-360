using GymApp.Common.Application.Messaging;
using GymApp.Common.Domain;
using GymApp.Modules.Gyms.Domain.Entities;
using GymApp.Modules.Gyms.Domain.Enums;
using GymApp.Modules.Gyms.Domain.Errors;
using GymApp.Modules.Gyms.Domain.Repositories;
using GymApp.Modules.Gyms.Application.Abstractions;
using GymApp.Modules.Gyms.Application.Services;
using GymApp.Modules.Gyms.IntegrationEvents;

namespace GymApp.Modules.Gyms.Application.Commands.AddTrainer;

internal sealed class AddTrainerCommandHandler : ICommandHandler<AddTrainerCommand>
{
    private readonly IGymRepository _gymRepository;
    private readonly GymSeatLimitChecker _seatLimits;
    private readonly IGymTrainerRequestRepository _trainerRequestRepository;
    private readonly IGymsUnitOfWork _unitOfWork;
    private readonly IIntegrationEventPublisher _integrationEvents;

    public AddTrainerCommandHandler(
        IGymRepository gymRepository,
        GymSeatLimitChecker seatLimits,
        IGymTrainerRequestRepository trainerRequestRepository,
        IGymsUnitOfWork unitOfWork,
        IIntegrationEventPublisher integrationEvents)
    {
        _gymRepository = gymRepository;
        _seatLimits = seatLimits;
        _trainerRequestRepository = trainerRequestRepository;
        _unitOfWork = unitOfWork;
        _integrationEvents = integrationEvents;
    }

    public async Task<Result> Handle(AddTrainerCommand request, CancellationToken cancellationToken)
    {
        var gym = await _gymRepository.GetByIdAsync(request.GymId, cancellationToken);
        if (gym is null)
            return Result.Failure(GymErrors.NotFound);

        var seat = await _seatLimits.EnsureTrainerSeatAsync(request.GymId, cancellationToken);
        if (seat.IsFailure)
            return seat;

        var result = gym.AddTrainer(request.TrainerId);
        if (result.IsFailure)
            return result;

        await ApproveTrainerRequestAsync(request, cancellationToken);

        _gymRepository.Update(gym);

        _integrationEvents.Enqueue(
            new TrainerAddedToGymIntegrationEvent(request.GymId, request.TrainerId));

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }

    // Adding a trainer directly also approves (or creates an approved) join request,
    // carrying over any bio/specializations supplied with the command.
    private async Task ApproveTrainerRequestAsync(AddTrainerCommand request, CancellationToken cancellationToken)
    {
        var existingRequest = await _trainerRequestRepository.GetAsync(
            request.GymId, request.TrainerId, cancellationToken);

        if (existingRequest is null)
        {
            var approvedRequest = GymTrainerRequest.CreateApproved(request.GymId, request.TrainerId);
            ApplyProfileUpdate(approvedRequest, request);
            await _trainerRequestRepository.AddAsync(approvedRequest, cancellationToken);
            return;
        }

        var wasApproved = existingRequest.Status == TrainerRequestStatus.Approved;
        if (wasApproved && !HasProfileUpdate(request))
            return;

        if (!wasApproved)
            existingRequest.Approve();
        ApplyProfileUpdate(existingRequest, request);
        _trainerRequestRepository.Update(existingRequest);
    }

    private static bool HasProfileUpdate(AddTrainerCommand request) =>
        request.Bio is not null || request.Specializations is not null;

    private static void ApplyProfileUpdate(GymTrainerRequest trainerRequest, AddTrainerCommand request)
    {
        if (HasProfileUpdate(request))
            trainerRequest.UpdateProfile(request.Specializations, request.Bio);
    }
}
