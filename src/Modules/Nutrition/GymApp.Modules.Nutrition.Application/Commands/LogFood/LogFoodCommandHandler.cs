using GymApp.Common.Application.Abstractions;
using GymApp.Common.Application.Errors;
using GymApp.Common.Application.Messaging;
using GymApp.Common.Domain;
using GymApp.Modules.Nutrition.Domain.Entities;
using GymApp.Modules.Nutrition.Domain.Enums;
using GymApp.Modules.Nutrition.Domain.Repositories;
using GymApp.Modules.Nutrition.Domain.ValueObjects;
using GymApp.Modules.Nutrition.Application.Abstractions;

namespace GymApp.Modules.Nutrition.Application.Commands.LogFood;

internal sealed class LogFoodCommandHandler : ICommandHandler<LogFoodCommand, Guid>
{
    private readonly IFoodLogRepository _foodLogRepository;
    private readonly IGymPlanService _gymPlanService;
    private readonly INutritionUnitOfWork _unitOfWork;

    public LogFoodCommandHandler(
        IFoodLogRepository foodLogRepository,
        IGymPlanService gymPlanService,
        INutritionUnitOfWork unitOfWork)
    {
        _foodLogRepository = foodLogRepository;
        _gymPlanService = gymPlanService;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<Guid>> Handle(LogFoodCommand request, CancellationToken cancellationToken)
    {
        bool isEnabled = await _gymPlanService.IsFeatureEnabledAsync(
            request.GymId, "nutrition_tracking", cancellationToken);

        if (!isEnabled)
        {
            return Result.Failure<Guid>(PlanErrors.FeatureNotAvailable(
                "nutrition_tracking",
                await _gymPlanService.GetPlanCodeAsync(request.GymId, cancellationToken),
                "Starter"));
        }

        if (!Enum.TryParse<MealType>(request.MealType, ignoreCase: true, out var mealType))
        {
            return Result.Failure<Guid>(new Error(
                "Nutrition.InvalidMealType",
                $"'{request.MealType}' is not a valid meal type."));
        }

        var foodLog = await _foodLogRepository.GetByUserAndDateAsync(
            request.UserId, request.GymId, request.Date, cancellationToken);

        if (foodLog is null)
        {
            foodLog = FoodLog.Create(request.UserId, request.GymId, request.Date);
            await _foodLogRepository.AddAsync(foodLog, cancellationToken);
        }

        var foodItems = request.FoodItems.Select(fi =>
            new FoodItem(fi.Name, fi.Quantity, fi.Unit, fi.Calories, fi.Protein, fi.Carbs, fi.Fat));

        var meal = foodLog.AddMeal(mealType, foodItems);

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success(meal.Id);
    }
}
