using GymApp.Modules.Billing.Shared;
using GymApp.Common.Application.Errors;
using GymApp.Common.Application.Messaging;
using GymApp.Common.Domain;
using GymApp.Modules.Gyms.Application.Contracts;
using GymApp.Modules.Gyms.Domain.Entities;
using GymApp.Modules.Gyms.Domain.Errors;
using GymApp.Modules.Gyms.Domain.Repositories;
using GymApp.Modules.Gyms.Application.Abstractions;
using GymApp.Modules.Gyms.IntegrationEvents;

namespace GymApp.Modules.Gyms.Application.Commands.AddTrainer;

internal sealed class AddTrainerCommandHandler : ICommandHandler<AddTrainerCommand>
{
    private readonly IGymRepository _gymRepository;
    private readonly IGymPlanService _gymPlanService;
    private readonly IGymQueryService _gymQueryService;
    private readonly IGymTrainerRequestRepository _trainerRequestRepository;
    private readonly IGymsUnitOfWork _unitOfWork;
    private readonly IIntegrationEventPublisher _integrationEvents;

    public AddTrainerCommandHandler(
        IGymRepository gymRepository,
        IGymPlanService gymPlanService,
        IGymQueryService gymQueryService,
        IGymTrainerRequestRepository trainerRequestRepository,
        IGymsUnitOfWork unitOfWork,
        IIntegrationEventPublisher integrationEvents)
    {
        _gymRepository = gymRepository;
        _gymPlanService = gymPlanService;
        _gymQueryService = gymQueryService;
        _trainerRequestRepository = trainerRequestRepository;
        _unitOfWork = unitOfWork;
        _integrationEvents = integrationEvents;
    }

    public async Task<Result> Handle(AddTrainerCommand request, CancellationToken cancellationToken)
    {
        var gym = await _gymRepository.GetByIdAsync(request.GymId, cancellationToken);
        if (gym is null)
            return Result.Failure(GymErrors.NotFound);

        var trainerLimit = await _gymPlanService.GetTrainerLimitAsync(request.GymId, cancellationToken);
        var currentCount = await _gymQueryService.GetTrainerCountAsync(request.GymId, cancellationToken);

        if (trainerLimit > 0 && currentCount >= trainerLimit)
            return Result.Failure(PlanErrors.LimitExceeded("trainers", currentCount, trainerLimit));

        var result = gym.AddTrainer(request.TrainerId);
        if (result.IsFailure)
            return result;

        var existingRequest = await _trainerRequestRepository.GetAsync(
            request.GymId, request.TrainerId, cancellationToken);

        var hasProfileUpdate = request.Bio is not null || request.Specializations is not null;

        if (existingRequest is null)
        {
            var approvedRequest = GymTrainerRequest.CreateApproved(request.GymId, request.TrainerId);
            if (hasProfileUpdate)
                approvedRequest.UpdateProfile(request.Specializations, request.Bio);
            await _trainerRequestRepository.AddAsync(approvedRequest, cancellationToken);
        }
        else if (existingRequest.Status != Domain.Enums.TrainerRequestStatus.Approved)
        {
            existingRequest.Approve();
            if (hasProfileUpdate)
                existingRequest.UpdateProfile(request.Specializations, request.Bio);
            _trainerRequestRepository.Update(existingRequest);
        }
        else if (hasProfileUpdate)
        {
            existingRequest.UpdateProfile(request.Specializations, request.Bio);
            _trainerRequestRepository.Update(existingRequest);
        }

        _gymRepository.Update(gym);

        _integrationEvents.Enqueue(
            new TrainerAddedToGymIntegrationEvent(request.GymId, request.TrainerId));

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
