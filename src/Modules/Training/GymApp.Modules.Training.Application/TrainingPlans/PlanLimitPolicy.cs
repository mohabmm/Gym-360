using GymApp.Common.Domain;
using GymApp.Modules.Training.Domain.Enums;
using GymApp.Modules.Training.Domain.Errors;
using GymApp.Modules.Training.Domain.ValueObjects;

namespace GymApp.Modules.Training.Application.TrainingPlans;

/// <summary>
/// pt.starter_plan: what a coach may give a member under the pair's limit
/// (<see cref="PlanAccess.GetPlanLimitAsync"/>). Starter: a 3-day plan built in the builder
/// (no template). After a staff unlock: one week, or up to 4 weeks for 30 days, from a template or
/// ready program. With the flag off anything goes.
/// </summary>
internal static class PlanLimitPolicy
{
    /// <summary>Whether a plan with this content (copied from <paramref name="templateId"/>, if any) is allowed.</summary>
    public static Result Check(TrainingPlanLimit limit, Guid? templateId, PlanContent content)
    {
        if (limit == TrainingPlanLimit.Starter && templateId is not null)
            return Result.Failure(TrainingPlanErrors.StarterPlanOnly);
        return PlanLimitRules.Fits(limit, content.Document)
            ? Result.Success()
            : Result.Failure(TrainingPlanErrors.PlanTooLong(limit));
    }
}
