using GymApp.Common.Application.Messaging;
using GymApp.Common.Domain;
using GymApp.Modules.Training.Domain.Errors;
using GymApp.Modules.Training.Domain.Repositories;

namespace GymApp.Modules.Training.Application.TrainingPlans.Plans;

public sealed record GetTrainingPlanQuery(PlanActor Actor, Guid PlanId) : IQuery<TrainingPlanDto>;

/// <summary>A trainee's active plan (with its days) and every older plan, for their coach.</summary>
public sealed record GetTraineePlansQuery(PlanActor Actor, Guid TraineeId) : IQuery<TraineePlansDto>;

public sealed record TraineePlansDto(TrainingPlanDto? Active, IReadOnlyList<TrainingPlanSummaryDto> History);

internal sealed class TrainingPlanQueryHandler
    : IQueryHandler<GetTrainingPlanQuery, TrainingPlanDto>,
      IQueryHandler<GetTraineePlansQuery, TraineePlansDto>
{
    private readonly PlanAccess _access;
    private readonly ITrainingPlanRepository _plans;

    public TrainingPlanQueryHandler(PlanAccess access, ITrainingPlanRepository plans)
    {
        _access = access;
        _plans = plans;
    }

    public async Task<Result<TrainingPlanDto>> Handle(GetTrainingPlanQuery request, CancellationToken cancellationToken)
    {
        var enabled = await _access.EnsureEnabledAsync(request.Actor.GymId, cancellationToken);
        if (enabled.IsFailure)
            return Result.Failure<TrainingPlanDto>(enabled.Error);
        var plan = await _plans.GetByIdAsync(request.Actor.GymId, request.PlanId, cancellationToken);
        if (plan is null)
            return Result.Failure<TrainingPlanDto>(TrainingPlanErrors.PlanNotFound);
        return await _access.CanReadAsync(request.Actor, plan, cancellationToken)
            ? Result.Success(plan.ToDto())
            : PlanAccess.Forbidden<TrainingPlanDto>();
    }

    public async Task<Result<TraineePlansDto>> Handle(GetTraineePlansQuery request, CancellationToken cancellationToken)
    {
        var enabled = await _access.EnsureEnabledAsync(request.Actor.GymId, cancellationToken);
        if (enabled.IsFailure)
            return Result.Failure<TraineePlansDto>(enabled.Error);
        var plans = await _plans.ListForTraineeAsync(request.Actor.GymId, request.TraineeId, cancellationToken);
        if (!await CanSeeTraineeAsync(request, plans, cancellationToken))
            return PlanAccess.Forbidden<TraineePlansDto>();

        var active = plans.FirstOrDefault(p => p.IsActive);
        return Result.Success(new TraineePlansDto(
            active?.ToDto(),
            plans.Where(p => !p.IsActive).Select(p => p.ToSummary()).ToList()));
    }

    // Their current coach, a coach who once gave them a plan, or the gym's owner/manager.
    private async Task<bool> CanSeeTraineeAsync(
        GetTraineePlansQuery request, IReadOnlyList<Domain.Entities.TrainingPlan> plans, CancellationToken cancellationToken)
    {
        if (request.Actor.IsGymManager || plans.Any(p => p.TrainerId == request.Actor.UserId))
            return true;
        return await _access.IsCoachOfAsync(request.Actor, request.TraineeId, cancellationToken);
    }
}
