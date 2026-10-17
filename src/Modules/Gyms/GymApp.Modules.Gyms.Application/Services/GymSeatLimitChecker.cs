using GymApp.Common.Application.Errors;
using GymApp.Common.Domain;
using GymApp.Modules.Billing.Shared;
using GymApp.Modules.Gyms.Application.Contracts;
using GymApp.Modules.Gyms.Domain.ValueObjects;

namespace GymApp.Modules.Gyms.Application.Services;

/// <summary>
/// Enforces the plan's seat limits (members, trainers, staff) by comparing the
/// plan limit with the gym's current head count. A limit of 0 means unlimited.
/// </summary>
internal sealed class GymSeatLimitChecker
{
    private readonly IGymPlanService _gymPlanService;
    private readonly IGymQueryService _gymQueryService;

    public GymSeatLimitChecker(IGymPlanService gymPlanService, IGymQueryService gymQueryService)
    {
        _gymPlanService = gymPlanService;
        _gymQueryService = gymQueryService;
    }

    public async Task<Result> EnsureMemberSeatAsync(Guid gymId, CancellationToken cancellationToken)
    {
        var limit = await _gymPlanService.GetMemberLimitAsync(gymId, cancellationToken);
        var currentCount = await _gymQueryService.GetMemberCountAsync(new GymId(gymId), cancellationToken);
        return EnsureWithinLimit("members", currentCount, limit);
    }

    public async Task<Result> EnsureTrainerSeatAsync(Guid gymId, CancellationToken cancellationToken)
    {
        var limit = await _gymPlanService.GetTrainerLimitAsync(gymId, cancellationToken);
        var currentCount = await _gymQueryService.GetTrainerCountAsync(new GymId(gymId), cancellationToken);
        return EnsureWithinLimit("trainers", currentCount, limit);
    }

    public async Task<Result> EnsureStaffSeatAsync(Guid gymId, CancellationToken cancellationToken)
    {
        var limit = await _gymPlanService.GetStaffLimitAsync(gymId, cancellationToken);
        var currentCount = await _gymQueryService.GetStaffCountAsync(new GymId(gymId), cancellationToken);
        return EnsureWithinLimit("staff", currentCount, limit);
    }

    private static Result EnsureWithinLimit(string resource, int currentCount, int limit) =>
        limit > 0 && currentCount >= limit
            ? Result.Failure(PlanErrors.LimitExceeded(resource, currentCount, limit))
            : Result.Success();
}
