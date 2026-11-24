using GymApp.Common.Application.Messaging;
using GymApp.Modules.Nutrition.Application.DTOs;

namespace GymApp.Modules.Nutrition.Application.Diets;

/// <summary>The coach and the gym they are working in (from the token).</summary>
public sealed record DietCoach(Guid GymId, Guid TrainerId);

/// <summary>
/// What the coach enters in the calculator. <paramref name="Sex"/>: Male / Female.
/// <paramref name="Activity"/>: Low / Light / Moderate / High. <paramref name="Goal"/>: FatLoss / Maintain / MuscleGain.
/// </summary>
public sealed record BodyInput(string Sex, int Age, decimal HeightCm, decimal WeightKg, string Activity, string Goal);

public sealed record DailyTargetDto(int Bmr, int Maintenance, int Calories, int ProteinGrams, int CarbsGrams, int FatGrams, string Goal);

/// <summary>The daily target the plan is built for; the coach may have adjusted the calculator's numbers.</summary>
public sealed record DietTargetInput(int Calories, int ProteinGrams, int CarbsGrams, int FatGrams);

/// <summary>A ready diet in the picker. <paramref name="Goal"/> null = fits any goal.</summary>
public sealed record ReadyDietSummaryDto(
    string Key, string Name, string NameAr, string Description, string DescriptionAr, string? Goal, int MealsPerDay);

/// <summary>A ready diet scaled to <paramref name="Calories"/>, Saturday to Friday.</summary>
public sealed record ReadyDietPreviewDto(ReadyDietSummaryDto Summary, int Calories, List<MealPlanDayDto> Days);

/// <summary>One day of the last week: how many of the plan's meals the member ticked.</summary>
public sealed record DietDayAdherenceDto(DateOnly Date, int MealsDone, int MealsPlanned);

/// <summary>The member's current diet for their coach, and how the last 7 days went.</summary>
public sealed record ClientDietDto(MealPlanDetailDto? Plan, List<DietDayAdherenceDto> LastWeek);

/// <summary>One meal of today's plan; <paramref name="Done"/> once the member ticked it.</summary>
public sealed record TodayMealDto(int Index, PlannedMealDto Meal, PlannedMealDto? Alternative, bool Done);

/// <summary>The member's home card: today's meals against the day's target.</summary>
public sealed record TodayMealsDto(
    Guid PlanId, string PlanName, DateOnly Date, decimal TargetCalories, MacroTargetsDto Targets,
    List<TodayMealDto> Meals, decimal EatenCalories);

public sealed record CalculateCalorieTargetQuery(Guid GymId, BodyInput Body) : IQuery<DailyTargetDto>;

public sealed record ListReadyDietsQuery(Guid GymId, string? Goal) : IQuery<IReadOnlyList<ReadyDietSummaryDto>>;

public sealed record PreviewReadyDietQuery(Guid GymId, string Key, int Calories) : IQuery<ReadyDietPreviewDto>;

public sealed record GiveDietCommand(DietCoach Coach, Guid ClientId, string DietKey, DietTargetInput Target) : ICommand<MealPlanDetailDto>;

public sealed record GetClientDietQuery(DietCoach Coach, Guid ClientId) : IQuery<ClientDietDto>;

public sealed record GetTodayMealsQuery(Guid MemberId, Guid GymId) : IQuery<TodayMealsDto?>;

/// <param name="Alternative">True when the member ate the meal's alternative instead.</param>
public sealed record CompleteTodayMealCommand(Guid MemberId, Guid GymId, int Index, bool Alternative) : ICommand<TodayMealsDto>;
