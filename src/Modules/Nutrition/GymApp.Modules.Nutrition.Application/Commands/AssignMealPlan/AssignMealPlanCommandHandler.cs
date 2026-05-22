using GymApp.Modules.Billing.Shared;
using GymApp.Common.Application.Errors;
using GymApp.Common.Application.Messaging;
using GymApp.Common.Domain;
using GymApp.Modules.Nutrition.Domain.Errors;
using GymApp.Modules.Nutrition.Domain.Repositories;
using GymApp.Modules.Nutrition.Application.Abstractions;

namespace GymApp.Modules.Nutrition.Application.Commands.AssignMealPlan;

internal sealed class AssignMealPlanCommandHandler : ICommandHandler<AssignMealPlanCommand>
{
    private readonly IMealPlanRepository _mealPlanRepository;
    private readonly IGymPlanService _gymPlanService;
    private readonly INutritionUnitOfWork _unitOfWork;

    public AssignMealPlanCommandHandler(
        IMealPlanRepository mealPlanRepository,
        IGymPlanService gymPlanService,
        INutritionUnitOfWork unitOfWork)
    {
        _mealPlanRepository = mealPlanRepository;
        _gymPlanService = gymPlanService;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result> Handle(AssignMealPlanCommand request, CancellationToken cancellationToken)
    {
        var mealPlan = await _mealPlanRepository.GetByIdAsync(request.MealPlanId, cancellationToken);

        if (mealPlan is null)
            return Result.Failure(NutritionErrors.MealPlanNotFound(request.MealPlanId));

        bool isEnabled = await _gymPlanService.IsFeatureEnabledAsync(
            mealPlan.GymId, "meal_plans", cancellationToken);

        if (!isEnabled)
        {
            return Result.Failure(PlanErrors.FeatureNotAvailable(
                "meal_plans",
                await _gymPlanService.GetPlanCodeAsync(mealPlan.GymId, cancellationToken),
                "Pro"));
        }

        var result = mealPlan.AssignToClient(request.ClientId);

        if (result.IsFailure)
            return result;

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
