using GymApp.Common.Application.Messaging;
using GymApp.Common.Domain;
using GymApp.Modules.Nutrition.Application.Mappers;
using GymApp.Modules.Nutrition.Domain.Diet;

namespace GymApp.Modules.Nutrition.Application.Diets;

/// <summary>The coach's calculator and the ready-diet picker (no data of the member is read or kept).</summary>
internal sealed class DietCatalogQueryHandler
    : IQueryHandler<CalculateCalorieTargetQuery, DailyTargetDto>,
      IQueryHandler<ListReadyDietsQuery, IReadOnlyList<ReadyDietSummaryDto>>,
      IQueryHandler<PreviewReadyDietQuery, ReadyDietPreviewDto>
{
    public const int MinCalories = 1000;
    public const int MaxCalories = 5000;

    private readonly DietAccess _access;

    public DietCatalogQueryHandler(DietAccess access) => _access = access;

    public async Task<Result<DailyTargetDto>> Handle(CalculateCalorieTargetQuery request, CancellationToken cancellationToken)
    {
        var enabled = await _access.EnsureMealPlansAsync(request.GymId, cancellationToken);
        if (enabled.IsFailure)
            return Result.Failure<DailyTargetDto>(enabled.Error);
        var body = ToBody(request.Body);
        if (body.IsFailure)
            return Result.Failure<DailyTargetDto>(body.Error);
        var target = CalorieCalculator.Calculate(body.Value);
        return target.IsFailure
            ? Result.Failure<DailyTargetDto>(target.Error)
            : Result.Success(ToDto(target.Value, body.Value.Goal));
    }

    public async Task<Result<IReadOnlyList<ReadyDietSummaryDto>>> Handle(ListReadyDietsQuery request, CancellationToken cancellationToken)
    {
        var enabled = await _access.EnsureMealPlansAsync(request.GymId, cancellationToken);
        if (enabled.IsFailure)
            return Result.Failure<IReadOnlyList<ReadyDietSummaryDto>>(enabled.Error);
        return Result.Success<IReadOnlyList<ReadyDietSummaryDto>>(ReadyDietCatalog.ForGoal(Parse<DietGoal>(request.Goal)).Select(ToSummary).ToList());
    }

    public async Task<Result<ReadyDietPreviewDto>> Handle(PreviewReadyDietQuery request, CancellationToken cancellationToken)
    {
        var enabled = await _access.EnsureMealPlansAsync(request.GymId, cancellationToken);
        if (enabled.IsFailure)
            return Result.Failure<ReadyDietPreviewDto>(enabled.Error);
        var diet = ReadyDietCatalog.Find(request.Key);
        if (diet is null)
            return Result.Failure<ReadyDietPreviewDto>(DietErrors.DietNotFound);
        if (!IsValidCalories(request.Calories))
            return Result.Failure<ReadyDietPreviewDto>(DietErrors.InvalidTarget);
        var days = diet.ScaleTo(request.Calories).Select(ToDayDto).ToList();
        return Result.Success(new ReadyDietPreviewDto(ToSummary(diet), request.Calories, days));
    }

    public static bool IsValidCalories(int calories) => calories is >= MinCalories and <= MaxCalories;

    public static ReadyDietSummaryDto ToSummary(ReadyDiet d) => new(
        d.Key, d.Text.NameEn, d.Text.NameAr, d.Text.DescriptionEn, d.Text.DescriptionAr, d.Goal?.ToString(), d.MealsPerDay);

    public static DTOs.MealPlanDayDto ToDayDto(ScaledDietDay day) => new(
        day.DayOfWeek,
        day.Meals.Select(NutritionMapper.ToPlannedMealDto).ToList(),
        day.Alternatives.Select(NutritionMapper.ToPlannedMealDto).ToList());

    private static DailyTargetDto ToDto(DailyTarget t, DietGoal goal) =>
        new(t.Bmr, t.Maintenance, t.Calories, t.ProteinGrams, t.CarbsGrams, t.FatGrams, goal.ToString());

    private static Result<BodyProfile> ToBody(BodyInput input) =>
        (Parse<Sex>(input.Sex), Parse<ActivityLevel>(input.Activity), Parse<DietGoal>(input.Goal)) is ({ } sex, { } activity, { } goal)
            ? Result.Success(new BodyProfile(sex, input.Age, input.HeightCm, input.WeightKg, activity, goal))
            : Result.Failure<BodyProfile>(DietErrors.InvalidBody("Sex, activity or goal is not recognised."));

    /// <summary>A named value only ("7" is not an activity level); null otherwise.</summary>
    public static T? Parse<T>(string? value) where T : struct, Enum
    {
        if (int.TryParse(value, out _))
            return null;
        return Enum.TryParse<T>(value, ignoreCase: true, out var parsed) && Enum.IsDefined(parsed) ? parsed : null;
    }
}
