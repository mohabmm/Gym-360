using GymApp.Common.Application.Messaging;
using GymApp.Common.Domain;
using GymApp.Modules.Nutrition.Application.DTOs;
using GymApp.Modules.Nutrition.Application.Mappers;
using GymApp.Modules.Nutrition.Domain.Repositories;

namespace GymApp.Modules.Nutrition.Application.Queries.GetFoodLog;

internal sealed class GetFoodLogQueryHandler : IQueryHandler<GetFoodLogQuery, FoodLogResponse>
{
    private readonly IFoodLogRepository _foodLogRepository;

    public GetFoodLogQueryHandler(IFoodLogRepository foodLogRepository)
    {
        _foodLogRepository = foodLogRepository;
    }

    public async Task<Result<FoodLogResponse>> Handle(GetFoodLogQuery request, CancellationToken cancellationToken)
    {
        var foodLog = await _foodLogRepository.GetByUserAndDateAsync(
            request.UserId, request.GymId, request.Date, cancellationToken);

        // No log yet for this day is the expected state — return an empty response
        // so clients can render "0 kcal" instead of a 400, mirroring GetWaterLog.
        if (foodLog is null)
        {
            return Result.Success(new FoodLogResponse(
                Guid.Empty,
                request.UserId,
                request.Date,
                TotalCalories: 0m,
                TotalProtein: 0m,
                TotalCarbs: 0m,
                TotalFat: 0m,
                Meals: new List<MealEntryDto>()));
        }

        return Result.Success(NutritionMapper.ToFoodLogResponse(foodLog));
    }
}
