using GymApp.Common.Application.Messaging;
using GymApp.Common.Domain;
using GymApp.Modules.Training.Application.Abstractions;
using GymApp.Modules.Training.Domain.Entities;
using GymApp.Modules.Training.Domain.Enums;
using GymApp.Modules.Training.Domain.Errors;
using GymApp.Modules.Training.Domain.Repositories;
using GymApp.Modules.Training.Domain.ValueObjects;
using GymApp.Modules.Training.IntegrationEvents;

namespace GymApp.Modules.Training.Application.TrainingPlans.Plans;

internal sealed class AssignTrainingPlanCommandHandler : ICommandHandler<AssignTrainingPlanCommand, TrainingPlanDto>
{
    private readonly PlanAccess _access;
    private readonly IPlanTemplateRepository _templates;
    private readonly ITrainingPlanRepository _plans;
    private readonly IIntegrationEventPublisher _integrationEvents;
    private readonly ITrainingUnitOfWork _unitOfWork;

    public AssignTrainingPlanCommandHandler(
        PlanAccess access,
        IPlanTemplateRepository templates,
        ITrainingPlanRepository plans,
        IIntegrationEventPublisher integrationEvents,
        ITrainingUnitOfWork unitOfWork)
    {
        _access = access;
        _templates = templates;
        _plans = plans;
        _integrationEvents = integrationEvents;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<TrainingPlanDto>> Handle(AssignTrainingPlanCommand request, CancellationToken cancellationToken)
    {
        var enabled = await _access.EnsureEnabledAsync(request.Actor.GymId, cancellationToken);
        if (enabled.IsFailure)
            return Result.Failure<TrainingPlanDto>(enabled.Error);
        if (!await _access.IsCoachOfAsync(request.Actor, request.TraineeId, cancellationToken))
            return PlanAccess.Forbidden<TrainingPlanDto>();
        var content = await ResolveContentAsync(request, cancellationToken);
        if (content.IsFailure)
            return Result.Failure<TrainingPlanDto>(content.Error);
        var pair = new CoachTrainee(request.Actor.GymId, request.Actor.UserId, request.TraineeId);
        var limit = await _access.GetPlanLimitAsync(pair, cancellationToken);
        var allowed = PlanLimitPolicy.Check(limit, request.TemplateId, content.Value);
        if (allowed.IsFailure)
            return Result.Failure<TrainingPlanDto>(allowed.Error);
        if (limit is TrainingPlanLimit.OneWeek or TrainingPlanLimit.ThirtyDays)
            await _access.UseUnlockAsync(pair, cancellationToken);

        var plan = TrainingPlan.Assign(
            new PlanAssignment(pair.GymId, pair.TrainerId, pair.TraineeId, request.StartDate, request.TemplateId, limit),
            content.Value);
        await _unitOfWork.InTransactionAsync(ct => ReplaceActiveAndAddAsync(plan, ct), cancellationToken);
        return Result.Success(plan.ToDto());
    }

    private async Task<Result<PlanContent>> ResolveContentAsync(AssignTrainingPlanCommand request, CancellationToken cancellationToken)
    {
        if (request.TemplateId is { } templateId)
        {
            var template = await _templates.GetByIdAsync(request.Actor.GymId, templateId, cancellationToken);
            if (template?.IsAvailableTo(request.Actor.AsTemplateUser) != true)
                return Result.Failure<PlanContent>(TrainingPlanErrors.TemplateNotFound);
            if (request.Content is null)
                return Result.Success(template.Content);
        }
        return request.Content is null
            ? Result.Failure<PlanContent>(TrainingPlanErrors.InvalidDocument("Pick a template or build the plan."))
            : PlanContentFactory.From(request.Content);
    }

    // The old plan must be saved as Replaced before the new one is inserted: the
    // "one active plan per trainee" index is checked statement by statement.
    private async Task<bool> ReplaceActiveAndAddAsync(TrainingPlan plan, CancellationToken cancellationToken)
    {
        var current = await _plans.GetActiveForTraineeAsync(plan.GymId, plan.TraineeId, cancellationToken);
        if (current is not null)
        {
            current.Replace();
            await _unitOfWork.SaveChangesAsync(cancellationToken);
        }
        await _plans.AddAsync(plan, cancellationToken);
        _integrationEvents.Enqueue(new TrainingPlanAssignedIntegrationEvent(
            plan.Id, plan.GymId, plan.TrainerId, plan.TraineeId, plan.Name, plan.NameAr));
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return true;
    }
}
