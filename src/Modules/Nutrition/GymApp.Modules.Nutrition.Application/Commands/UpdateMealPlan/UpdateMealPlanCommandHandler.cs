using GymApp.Common.Application.Messaging;
using GymApp.Common.Domain;
using GymApp.Modules.Nutrition.Application.Mappers;
using GymApp.Modules.Nutrition.Domain.Entities;
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

        if (HasDetailChanges(request))
            mealPlan.UpdateDetails(MergeDetails(mealPlan, request));

        if (request.Days is not null)
        {
            // Clear existing days and rebuild — full replacement strategy
            foreach (var dayDto in request.Days)
                mealPlan.AddDay(NutritionInputMapper.ToMealPlanDay(mealPlan.Id, dayDto));
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }

    private static bool HasDetailChanges(UpdateMealPlanCommand request) =>
        request.Name is not null
        || request.DailyCalorieTarget.HasValue
        || request.MacroTargets is not null;

    private static MealPlanDetails MergeDetails(MealPlan mealPlan, UpdateMealPlanCommand request) =>
        new()
        {
            Name = request.Name ?? mealPlan.Name,
            Description = request.Description ?? mealPlan.Description,
            DailyCalorieTarget = request.DailyCalorieTarget ?? (int)mealPlan.DailyCalorieTarget,
            MacroTargets = request.MacroTargets is not null
                ? NutritionInputMapper.ToMacroTargets(request.MacroTargets)
                : mealPlan.MacroTargets
        };
}
