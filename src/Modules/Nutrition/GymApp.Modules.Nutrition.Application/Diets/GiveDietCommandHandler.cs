using GymApp.Common.Application.Messaging;
using GymApp.Common.Domain;
using GymApp.Modules.Nutrition.Application.Abstractions;
using GymApp.Modules.Nutrition.Application.DTOs;
using GymApp.Modules.Nutrition.Application.Mappers;
using GymApp.Modules.Nutrition.Domain.Diet;
using GymApp.Modules.Nutrition.Domain.Entities;
using GymApp.Modules.Nutrition.Domain.Repositories;
using GymApp.Modules.Nutrition.Domain.ValueObjects;
using GymApp.Modules.Nutrition.IntegrationEvents;

namespace GymApp.Modules.Nutrition.Application.Diets;

/// <summary>
/// The coach gives a client a ready diet scaled to their target: a new meal plan of 7 days is
/// made for the client (the newest plan is the one the member sees) and the member is notified.
/// </summary>
internal sealed class GiveDietCommandHandler : ICommandHandler<GiveDietCommand, MealPlanDetailDto>
{
    private readonly DietAccess _access;
    private readonly IMealPlanRepository _plans;
    private readonly IIntegrationEventPublisher _events;
    private readonly INutritionUnitOfWork _unitOfWork;

    public GiveDietCommandHandler(DietAccess access, IMealPlanRepository plans, IIntegrationEventPublisher events, INutritionUnitOfWork unitOfWork)
    {
        _access = access;
        _plans = plans;
        _events = events;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<MealPlanDetailDto>> Handle(GiveDietCommand request, CancellationToken cancellationToken)
    {
        var allowed = await _access.EnsureCoachOfAsync(request.Coach, request.ClientId, cancellationToken);
        if (allowed.IsFailure)
            return Result.Failure<MealPlanDetailDto>(allowed.Error);
        var diet = ReadyDietCatalog.Find(request.DietKey);
        if (diet is null)
            return Result.Failure<MealPlanDetailDto>(DietErrors.DietNotFound);
        if (!DietCatalogQueryHandler.IsValidCalories(request.Target.Calories))
            return Result.Failure<MealPlanDetailDto>(DietErrors.InvalidTarget);

        var plan = Build(request, diet);
        var assigned = plan.AssignToClient(request.ClientId);
        if (assigned.IsFailure)
            return Result.Failure<MealPlanDetailDto>(assigned.Error);
        await _plans.AddAsync(plan, cancellationToken);
        _events.Enqueue(new MealPlanAssignedIntegrationEvent(
            plan.Id, request.Coach.GymId, request.Coach.TrainerId, request.ClientId, plan.Name, request.Target.Calories));
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return Result.Success(NutritionMapper.ToMealPlanDetailDto(plan));
    }

    private static MealPlan Build(GiveDietCommand request, ReadyDiet diet)
    {
        var target = request.Target;
        var plan = MealPlan.Create(request.Coach.TrainerId, request.Coach.GymId, new MealPlanDetails
        {
            Name = diet.Text.NameAr,
            Description = diet.Text.NameEn,
            DailyCalorieTarget = target.Calories,
            MacroTargets = new MacroTargets(target.ProteinGrams, target.CarbsGrams, target.FatGrams)
        }, isTemplate: false);
        foreach (var day in diet.ScaleTo(target.Calories))
            plan.AddDay(MealPlanDay.Create(plan.Id, day.DayOfWeek, day.Meals.Select(Copy), day.Alternatives.Select(Copy)));
        return plan;
    }

    /// <summary>
    /// Days A repeat through the week; EF tracks owned meals by reference, so each day gets its
    /// own copies (a shared instance would be saved on one day only).
    /// </summary>
    private static PlannedMeal Copy(PlannedMeal meal) =>
        meal with { FoodItems = meal.FoodItems.Select(f => f with { }).ToList() };
}
