using GymApp.Common.Application.Messaging;
using GymApp.Common.Domain;
using GymApp.Modules.Training.Application.Abstractions;
using GymApp.Modules.Training.Domain.Errors;
using GymApp.Modules.Training.Domain.Repositories;
using GymApp.Modules.Training.IntegrationEvents;

namespace GymApp.Modules.Training.Application.TrainingPlans.Plans;

internal sealed class UpdateTrainingPlanCommandHandler : ICommandHandler<UpdateTrainingPlanCommand, TrainingPlanDto>
{
    private readonly PlanAccess _access;
    private readonly ITrainingPlanRepository _plans;
    private readonly IIntegrationEventPublisher _integrationEvents;
    private readonly ITrainingUnitOfWork _unitOfWork;

    public UpdateTrainingPlanCommandHandler(
        PlanAccess access,
        ITrainingPlanRepository plans,
        IIntegrationEventPublisher integrationEvents,
        ITrainingUnitOfWork unitOfWork)
    {
        _access = access;
        _plans = plans;
        _integrationEvents = integrationEvents;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<TrainingPlanDto>> Handle(UpdateTrainingPlanCommand request, CancellationToken cancellationToken)
    {
        var enabled = await _access.EnsureEnabledAsync(request.Actor.GymId, cancellationToken);
        if (enabled.IsFailure)
            return Result.Failure<TrainingPlanDto>(enabled.Error);
        var plan = await _plans.GetByIdAsync(request.Actor.GymId, request.PlanId, cancellationToken);
        if (plan is null)
            return Result.Failure<TrainingPlanDto>(TrainingPlanErrors.PlanNotFound);
        if (!await _access.CanManageAsync(request.Actor, plan, cancellationToken))
            return PlanAccess.Forbidden<TrainingPlanDto>();
        var content = PlanContentFactory.From(request.Content);
        if (content.IsFailure)
            return Result.Failure<TrainingPlanDto>(content.Error);

        var revised = plan.Revise(content.Value, request.Actor.UserId, request.ExpectedVersion);
        if (revised.IsFailure)
            return Result.Failure<TrainingPlanDto>(revised.Error);
        _integrationEvents.Enqueue(new TrainingPlanUpdatedIntegrationEvent(
            plan.Id, plan.GymId, plan.TrainerId, plan.TraineeId, plan.Version, plan.Name, plan.NameAr));
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return Result.Success(plan.ToDto());
    }
}
