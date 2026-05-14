using GymApp.Common.Application.Messaging;
using GymApp.Common.Domain;
using GymApp.Modules.Nutrition.Application.DTOs;
using GymApp.Modules.Nutrition.Application.Mappers;
using GymApp.Modules.Nutrition.Domain.Repositories;

namespace GymApp.Modules.Nutrition.Application.Queries.GetClientFoodLog;

internal sealed class GetClientFoodLogQueryHandler : IQueryHandler<GetClientFoodLogQuery, FoodLogResponse>
{
    private readonly IFoodLogRepository _foodLogRepository;

    public GetClientFoodLogQueryHandler(IFoodLogRepository foodLogRepository)
    {
        _foodLogRepository = foodLogRepository;
    }

    public async Task<Result<FoodLogResponse>> Handle(
        GetClientFoodLogQuery request,
        CancellationToken cancellationToken)
    {
        var foodLog = await _foodLogRepository.GetByUserAndDateAsync(
            request.ClientId, request.GymId, request.Date, cancellationToken);

        if (foodLog is null)
        {
            return Result.Success(new FoodLogResponse(
                Guid.Empty,
                request.ClientId,
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
