using GymApp.Common.Application.Messaging;
using GymApp.Common.Domain;
using GymApp.Modules.Nutrition.Application.Abstractions;
using GymApp.Modules.Nutrition.Application.Mappers;
using GymApp.Modules.Nutrition.Domain.Diet;
using GymApp.Modules.Nutrition.Domain.Entities;
using GymApp.Modules.Nutrition.Domain.Repositories;
using GymApp.Modules.Nutrition.Domain.ValueObjects;

namespace GymApp.Modules.Nutrition.Application.Diets;

/// <summary>
/// The member's "أكل النهارده" card: today's meals from their current plan and a one-tap tick
/// that logs the meal into today's food log (once — a second tap changes nothing). The coach
/// sees the week in <see cref="GetClientDietQuery"/>.
/// </summary>
internal sealed class MemberDietHandler
    : IQueryHandler<GetTodayMealsQuery, TodayMealsDto?>,
      ICommandHandler<CompleteTodayMealCommand, TodayMealsDto>,
      IQueryHandler<GetClientDietQuery, ClientDietDto>
{
    private const int WeekDays = 7;

    private readonly DietAccess _access;
    private readonly IMealPlanRepository _plans;
    private readonly IFoodLogRepository _logs;
    private readonly INutritionUnitOfWork _unitOfWork;

    public MemberDietHandler(DietAccess access, IMealPlanRepository plans, IFoodLogRepository logs, INutritionUnitOfWork unitOfWork)
    {
        _access = access;
        _plans = plans;
        _logs = logs;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<TodayMealsDto?>> Handle(GetTodayMealsQuery request, CancellationToken cancellationToken)
    {
        var enabled = await _access.EnsureTrackingAsync(request.GymId, cancellationToken);
        if (enabled.IsFailure)
            return Result.Failure<TodayMealsDto?>(enabled.Error);
        var plan = await CurrentPlanAsync(request.MemberId, request.GymId, cancellationToken);
        if (plan is null)
            return Result.Success<TodayMealsDto?>(null);
        var today = DietCalendar.Today();
        var log = await _logs.GetByUserAndDateAsync(request.MemberId, request.GymId, today, cancellationToken);
        return Result.Success<TodayMealsDto?>(ToToday(plan, today, log));
    }

    public async Task<Result<TodayMealsDto>> Handle(CompleteTodayMealCommand request, CancellationToken cancellationToken)
    {
        var plan = await MemberPlanAsync(request.MemberId, request.GymId, cancellationToken);
        if (plan.IsFailure)
            return Result.Failure<TodayMealsDto>(plan.Error);
        var today = DietCalendar.Today();
        var slot = MealSlot.At(DayOf(plan.Value, today), request.Index);
        if (slot is null)
            return Result.Failure<TodayMealsDto>(DietErrors.MealNotFound);

        var log = await LogFor(request, today, cancellationToken);
        if (!MealsDone.IsDone(log, slot.Meal, slot.Alternative))
        {
            var eaten = slot.Eaten(request.Alternative);
            log.AddMeal(eaten.MealType, eaten.FoodItems);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
        }
        return Result.Success(ToToday(plan.Value, today, log));
    }

    /// <summary>Tracking is on and the member has a plan.</summary>
    private async Task<Result<MealPlan>> MemberPlanAsync(Guid memberId, Guid gymId, CancellationToken cancellationToken)
    {
        var enabled = await _access.EnsureTrackingAsync(gymId, cancellationToken);
        if (enabled.IsFailure)
            return Result.Failure<MealPlan>(enabled.Error);
        var plan = await CurrentPlanAsync(memberId, gymId, cancellationToken);
        return plan is null ? Result.Failure<MealPlan>(DietErrors.NoPlan) : Result.Success(plan);
    }

    /// <summary>A meal of the day and its alternative; null when the day has no meal at that position.</summary>
    private sealed record MealSlot(PlannedMeal Meal, PlannedMeal? Alternative)
    {
        public static MealSlot? At(MealPlanDay? day, int index)
        {
            var meal = day?.PlannedMeals.ElementAtOrDefault(index);
            return meal is null ? null : new MealSlot(meal, MealsDone.AlternativeAt(day!, index));
        }

        /// <summary>What the member ate: the alternative when they said so and there is one.</summary>
        public PlannedMeal Eaten(bool alternative) => alternative ? Alternative ?? Meal : Meal;
    }

    public async Task<Result<ClientDietDto>> Handle(GetClientDietQuery request, CancellationToken cancellationToken)
    {
        var allowed = await _access.EnsureCoachOfAsync(request.Coach, request.ClientId, cancellationToken);
        if (allowed.IsFailure)
            return Result.Failure<ClientDietDto>(allowed.Error);
        var plan = await CurrentPlanAsync(request.ClientId, request.Coach.GymId, cancellationToken);
        if (plan is null)
            return Result.Success(new ClientDietDto(null, new List<DietDayAdherenceDto>()));

        var today = DietCalendar.Today();
        var from = today.AddDays(1 - WeekDays);
        var logs = await _logs.GetByUserAsync(request.ClientId, request.Coach.GymId, new DateRange(from, today), cancellationToken);
        var week = Enumerable.Range(0, WeekDays).Select(i => from.AddDays(i)).Select(date =>
        {
            var day = DayOf(plan, date);
            var log = logs.FirstOrDefault(l => l.Date == date);
            return new DietDayAdherenceDto(date, day is null ? 0 : MealsDone.CountDone(log, day), day?.PlannedMeals.Count ?? 0);
        }).ToList();
        return Result.Success(new ClientDietDto(NutritionMapper.ToMealPlanDetailDto(plan), week));
    }

    /// <summary>The newest plan given to the member in this gym (as GET me/meal-plan shows it).</summary>
    private async Task<MealPlan?> CurrentPlanAsync(Guid memberId, Guid gymId, CancellationToken cancellationToken) =>
        (await _plans.GetByClientAsync(memberId, gymId, cancellationToken))
            .Where(p => !p.IsTemplate && p.ClientId == memberId)
            .OrderByDescending(p => p.UpdatedAt)
            .ThenByDescending(p => p.CreatedAt)
            .FirstOrDefault();

    private async Task<FoodLog> LogFor(CompleteTodayMealCommand request, DateOnly today, CancellationToken cancellationToken)
    {
        var log = await _logs.GetByUserAndDateAsync(request.MemberId, request.GymId, today, cancellationToken);
        if (log is not null)
            return log;
        log = FoodLog.Create(request.MemberId, request.GymId, today);
        await _logs.AddAsync(log, cancellationToken);
        return log;
    }

    private static MealPlanDay? DayOf(MealPlan plan, DateOnly date) => plan.Days.FirstOrDefault(d => d.DayOfWeek == date.DayOfWeek);

    private static TodayMealsDto ToToday(MealPlan plan, DateOnly today, FoodLog? log)
    {
        var day = DayOf(plan, today);
        var meals = day is null
            ? new List<TodayMealDto>()
            : day.PlannedMeals.Select((meal, i) =>
            {
                var alternative = MealsDone.AlternativeAt(day, i);
                return new TodayMealDto(i, NutritionMapper.ToPlannedMealDto(meal),
                    alternative is null ? null : NutritionMapper.ToPlannedMealDto(alternative), MealsDone.IsDone(log, meal, alternative));
            }).ToList();
        return new TodayMealsDto(plan.Id, plan.Name, today, plan.DailyCalorieTarget,
            NutritionMapper.ToMacroTargetsDto(plan.MacroTargets), meals, log?.DailyTotals.TotalCalories ?? 0);
    }
}
