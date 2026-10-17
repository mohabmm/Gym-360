using GymApp.Modules.Billing.Shared;
using GymApp.Common.Application.Errors;
using GymApp.Common.Application.Messaging;
using GymApp.Common.Domain;
using GymApp.Modules.Nutrition.Application.Mappers;
using GymApp.Modules.Nutrition.Domain.Entities;
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

        var mealPlan = MealPlan.Create(
            request.TrainerId,
            request.GymId,
            new MealPlanDetails
            {
                Name = request.Name,
                Description = request.Description,
                DailyCalorieTarget = request.DailyCalorieTarget,
                MacroTargets = NutritionInputMapper.ToMacroTargets(request.MacroTargets)
            },
            request.IsTemplate);

        foreach (var dayDto in request.Days)
            mealPlan.AddDay(NutritionInputMapper.ToMealPlanDay(mealPlan.Id, dayDto));

        await _mealPlanRepository.AddAsync(mealPlan, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success(mealPlan.Id);
    }

}
