using GymApp.Common.Application.Messaging;
using GymApp.Common.Domain;
using GymApp.Modules.Nutrition.Application.DTOs;
using GymApp.Modules.Nutrition.Application.Mappers;
using GymApp.Modules.Nutrition.Domain.Repositories;

namespace GymApp.Modules.Nutrition.Application.Queries.GetMealPlans;

internal sealed class GetMealPlansQueryHandler : IQueryHandler<GetMealPlansQuery, List<MealPlanDto>>
{
    private readonly IMealPlanRepository _mealPlanRepository;

    public GetMealPlansQueryHandler(IMealPlanRepository mealPlanRepository)
    {
        _mealPlanRepository = mealPlanRepository;
    }

    public async Task<Result<List<MealPlanDto>>> Handle(GetMealPlansQuery request, CancellationToken cancellationToken)
    {
        var plans = await _mealPlanRepository.GetByTrainerAsync(
            request.TrainerId, request.GymId, cancellationToken);

        var dtos = plans.Select(NutritionMapper.ToMealPlanDto).ToList();

        return Result.Success(dtos);
    }
}
