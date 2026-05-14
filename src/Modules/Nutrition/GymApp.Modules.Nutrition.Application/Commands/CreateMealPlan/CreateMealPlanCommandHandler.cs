using GymApp.Common.Application.Abstractions;
using GymApp.Common.Application.Errors;
using GymApp.Common.Application.Messaging;
using GymApp.Common.Domain;
using GymApp.Modules.Nutrition.Application.DTOs;
using GymApp.Modules.Nutrition.Domain.Entities;
using GymApp.Modules.Nutrition.Domain.Enums;
using GymApp.Modules.Nutrition.Domain.Repositories;
using GymApp.Modules.Nutrition.Domain.ValueObjects;
using GymApp.Modules.Nutrition.Application.Abstractions;

namespace GymApp.Modules.Nutrition.Application.Commands.CreateMealPlan;

internal sealed class CreateMealPlanCommandHandler : ICommandHandler<CreateMealPlanCommand, Guid>
{
    private readonly IMealPlanRepository _mealPlanRepository;
    private readonly IGymPlanService _gymPlanService;
    private readonly INutritionUnitOfWork _unitOfWork;

    public CreateMealPlanCommandHandler(
        IMealPlanRepository mealPlanRepository,
        IGymPlanService gymPlanService,
        INutritionUnitOfWork unitOfWork)
    {
        _mealPlanRepository = mealPlanRepository;
        _gymPlanService = gymPlanService;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<Guid>> Handle(CreateMealPlanCommand request, CancellationToken cancellationToken)
    {
        bool isEnabled = await _gymPlanService.IsFeatureEnabledAsync(
            request.GymId, "meal_plans", cancellationToken);

        if (!isEnabled)
        {
            return Result.Failure<Guid>(PlanErrors.FeatureNotAvailable(
                "meal_plans",
                await _gymPlanService.GetPlanCodeAsync(request.GymId, cancellationToken),
                "Pro"));
        }

        var macroTargets = new MacroTargets(
            request.MacroTargets.ProteinGrams,
            request.MacroTargets.CarbsGrams,
            request.MacroTargets.FatGrams);

        var mealPlan = MealPlan.Create(
            request.TrainerId,
            request.GymId,
            request.Name,
            request.Description,
            request.DailyCalorieTarget,
            macroTargets,
            request.IsTemplate);

        foreach (var dayDto in request.Days)
        {
            var plannedMeals = MapPlannedMeals(dayDto.PlannedMeals);
            var alternatives = dayDto.Alternatives is not null
                ? MapPlannedMeals(dayDto.Alternatives)
                : null;

            var day = MealPlanDay.Create(
                mealPlan.Id,
                dayDto.DayOfWeek,
                plannedMeals,
                alternatives);

            mealPlan.AddDay(day);
        }

        await _mealPlanRepository.AddAsync(mealPlan, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success(mealPlan.Id);
    }

    private static List<PlannedMeal> MapPlannedMeals(List<PlannedMealDto> dtos) =>
        dtos.Select(pm => new PlannedMeal(
            Enum.Parse<MealType>(pm.MealType, ignoreCase: true),
            pm.Name,
            pm.Description,
            pm.FoodItems.Select(fi =>
                new FoodItem(fi.Name, fi.Quantity, fi.Unit, fi.Calories, fi.Protein, fi.Carbs, fi.Fat))
                .ToList(),
            pm.TotalCalories,
            pm.TotalProtein,
            pm.TotalCarbs,
            pm.TotalFat))
        .ToList();
}
