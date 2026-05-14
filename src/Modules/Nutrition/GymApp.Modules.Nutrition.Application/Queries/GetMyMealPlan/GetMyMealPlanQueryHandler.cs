using GymApp.Common.Application.Abstractions;
using GymApp.Common.Application.Errors;
using GymApp.Common.Application.Messaging;
using GymApp.Common.Domain;
using GymApp.Modules.Nutrition.Application.DTOs;
using GymApp.Modules.Nutrition.Application.Mappers;
using GymApp.Modules.Nutrition.Domain.Repositories;

namespace GymApp.Modules.Nutrition.Application.Queries.GetMyMealPlan;

internal sealed class GetMyMealPlanQueryHandler
    : IQueryHandler<GetMyMealPlanQuery, MealPlanDetailDto?>
{
    private readonly IMealPlanRepository _mealPlanRepository;
    private readonly IGymMembershipChecker _membershipChecker;

    public GetMyMealPlanQueryHandler(
        IMealPlanRepository mealPlanRepository,
        IGymMembershipChecker membershipChecker)
    {
        _mealPlanRepository = mealPlanRepository;
        _membershipChecker = membershipChecker;
    }

    public async Task<Result<MealPlanDetailDto?>> Handle(
        GetMyMealPlanQuery request,
        CancellationToken cancellationToken)
    {
        // Suspension check runs before membership check so a suspended gym does
        // not leak membership information (consistent with PRODUCT_SPEC §5).
        if (await _membershipChecker.IsGymSuspendedAsync(request.GymId, cancellationToken))
            return Result.Failure<MealPlanDetailDto?>(GymAccessErrors.GymSuspended);

        if (!await _membershipChecker.IsActiveMemberAsync(request.GymId, request.UserId, cancellationToken))
            return Result.Failure<MealPlanDetailDto?>(GymAccessErrors.MemberRoleRequired);

        var plans = await _mealPlanRepository.GetByClientAsync(
            request.UserId, request.GymId, cancellationToken);

        // Most-recently-assigned, non-template plan. MealPlan.UpdatedAt is set
        // at assignment time (AssignToClient), so we sort on UpdatedAt DESC and
        // fall back to CreatedAt for a stable order.
        var mostRecent = plans
            .Where(p => !p.IsTemplate && p.ClientId == request.UserId)
            .OrderByDescending(p => p.UpdatedAt)
            .ThenByDescending(p => p.CreatedAt)
            .FirstOrDefault();

        if (mostRecent is null)
            return Result.Success<MealPlanDetailDto?>(null);

        return Result.Success<MealPlanDetailDto?>(NutritionMapper.ToMealPlanDetailDto(mostRecent));
    }
}
