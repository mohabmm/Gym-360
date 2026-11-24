using GymApp.Common.Application.Messaging;
using GymApp.Common.Domain;
using GymApp.Modules.Training.Domain.Entities;
using GymApp.Modules.Training.Domain.Repositories;

namespace GymApp.Modules.Training.Application.TrainingPlans.Starter;

/// <summary>The coach's view: a 3-day plan, then one week each time the staff unlock it.</summary>
internal sealed class GetCoachPlanAccessQueryHandler : IQueryHandler<GetCoachPlanAccessQuery, CoachPlanAccessDto>
{
    private readonly PlanAccess _access;
    private readonly ITrainingPlanRepository _plans;

    public GetCoachPlanAccessQueryHandler(PlanAccess access, ITrainingPlanRepository plans)
    {
        _access = access;
        _plans = plans;
    }

    public async Task<Result<CoachPlanAccessDto>> Handle(GetCoachPlanAccessQuery request, CancellationToken cancellationToken)
    {
        var enabled = await _access.EnsureEnabledAsync(request.Actor.GymId, cancellationToken);
        if (enabled.IsFailure)
            return Result.Failure<CoachPlanAccessDto>(enabled.Error);
        if (!await _access.IsCoachOfAsync(request.Actor, request.TraineeId, cancellationToken))
            return PlanAccess.Forbidden<CoachPlanAccessDto>();

        var pair = new CoachTrainee(request.Actor.GymId, request.Actor.UserId, request.TraineeId);
        var limit = await _access.GetPlanLimitAsync(pair, cancellationToken);
        var active = await _plans.GetActiveForTraineeAsync(pair.GymId, pair.TraineeId, cancellationToken);
        return Result.Success(StarterStatus.Access(limit, active));
    }
}
