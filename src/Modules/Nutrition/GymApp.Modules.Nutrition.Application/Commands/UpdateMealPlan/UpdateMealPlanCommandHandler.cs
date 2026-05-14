using GymApp.Common.Application.Messaging;
using GymApp.Common.Domain;
using GymApp.Modules.Nutrition.Application.DTOs;
using GymApp.Modules.Nutrition.Domain.Entities;
using GymApp.Modules.Nutrition.Domain.Enums;
using GymApp.Modules.Nutrition.Domain.Errors;
using GymApp.Modules.Nutrition.Domain.Repositories;
using GymApp.Modules.Nutrition.Domain.ValueObjects;
using GymApp.Modules.Nutrition.Application.Abstractions;

namespace GymApp.Modules.Nutrition.Application.Commands.UpdateMealPlan;

internal sealed class UpdateMealPlanCommandHandler : ICommandHandler<UpdateMealPlanCommand>
{
    private readonly IMealPlanRepository _mealPlanRepository;
    private readonly INutritionUnitOfWork _unitOfWork;

    public UpdateMealPlanCommandHandler(
        IMealPlanRepository mealPlanRepository,
        INutritionUnitOfWork unitOfWork)
    {
        _mealPlanRepository = mealPlanRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result> Handle(UpdateMealPlanCommand request, CancellationToken cancellationToken)
    {
        var mealPlan = await _mealPlanRepository.GetByIdAsync(request.MealPlanId, cancellationToken);

        if (mealPlan is null)
            return Result.Failure(NutritionErrors.MealPlanNotFound(request.MealPlanId));

        if (request.Name is not null || request.DailyCalorieTarget.HasValue || request.MacroTargets is not null)
        {
            var macroTargets = request.MacroTargets is not null
                ? new MacroTargets(
                    request.MacroTargets.ProteinGrams,
                    request.MacroTargets.CarbsGrams,
                    request.MacroTargets.FatGrams)
                : mealPlan.MacroTargets;

            mealPlan.UpdateDetails(
                request.Name ?? mealPlan.Name,
                request.Description ?? mealPlan.Description,
                request.DailyCalorieTarget ?? (int)mealPlan.DailyCalorieTarget,
                macroTargets);
        }

        if (request.Days is not null)
        {
            // Clear existing days and rebuild — full replacement strategy
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
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success();
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
