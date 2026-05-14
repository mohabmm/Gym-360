using GymApp.Common.Application.Messaging;
using GymApp.Common.Domain;
using GymApp.Modules.Nutrition.Application.DTOs;
using GymApp.Modules.Nutrition.Application.Mappers;
using GymApp.Modules.Nutrition.Domain.Errors;
using GymApp.Modules.Nutrition.Domain.Repositories;

namespace GymApp.Modules.Nutrition.Application.Queries.GetMealPlanDetail;

internal sealed class GetMealPlanDetailQueryHandler : IQueryHandler<GetMealPlanDetailQuery, MealPlanDetailDto>
{
    private readonly IMealPlanRepository _mealPlanRepository;

    public GetMealPlanDetailQueryHandler(IMealPlanRepository mealPlanRepository)
    {
        _mealPlanRepository = mealPlanRepository;
    }

    public async Task<Result<MealPlanDetailDto>> Handle(
        GetMealPlanDetailQuery request,
        CancellationToken cancellationToken)
    {
        var mealPlan = await _mealPlanRepository.GetByIdAsync(request.MealPlanId, cancellationToken);

        if (mealPlan is null)
            return Result.Failure<MealPlanDetailDto>(NutritionErrors.MealPlanNotFound(request.MealPlanId));

        return Result.Success(NutritionMapper.ToMealPlanDetailDto(mealPlan));
    }
}
