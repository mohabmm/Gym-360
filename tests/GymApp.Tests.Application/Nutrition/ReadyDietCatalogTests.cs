using GymApp.Modules.Nutrition.Application.Diets;
using GymApp.Modules.Nutrition.Domain.Diet;

namespace GymApp.Tests.Application.Nutrition;

/// <summary>
/// Ready diets are content scaled to a member's calories: every day must land near the target
/// after portions are rounded, alternatives near the meal they replace, Arabic first.
/// </summary>
public class ReadyDietCatalogTests
{
    public static TheoryData<string, int, decimal> Targets()
    {
        var data = new TheoryData<string, int, decimal>();
        foreach (var diet in ReadyDietCatalog.All)
        {
            data.Add(diet.Key, 1200, 0.08m);
            for (var kcal = 1400; kcal <= 3600; kcal += 200)
                data.Add(diet.Key, kcal, 0.05m);
        }
        return data;
    }

    [Theory]
    [MemberData(nameof(Targets))]
    public void Every_day_lands_near_the_target_after_rounding(string key, int calories, decimal tolerance)
    {
        var week = ReadyDietCatalog.Find(key)!.ScaleTo(calories);

        week.Should().HaveCount(7);
        week.Should().OnlyContain(d => Math.Abs(d.TotalCalories - calories) <= calories * tolerance);
    }

    [Fact]
    public void Week_starts_on_saturday_and_alternates_two_days()
    {
        var week = ReadyDietCatalog.Find("balanced_egyptian")!.ScaleTo(2000);

        week[0].DayOfWeek.Should().Be(DayOfWeek.Saturday);
        week[6].DayOfWeek.Should().Be(DayOfWeek.Friday);
        week[0].Meals[0].Name.Should().Be(week[2].Meals[0].Name);
        week[0].Meals[0].Name.Should().NotBe(week[1].Meals[0].Name);
    }

    [Fact]
    public void Portions_scale_with_the_target_and_keep_real_units()
    {
        var small = ReadyDietCatalog.Find("fat_loss_high_protein")!.ScaleTo(1400)[0].Meals[2];
        var large = ReadyDietCatalog.Find("fat_loss_high_protein")!.ScaleTo(2800)[0].Meals[2];

        var chickenSmall = small.FoodItems.Single(f => f.Name.StartsWith("فراخ"));
        var chickenLarge = large.FoodItems.Single(f => f.Name.StartsWith("فراخ"));
        chickenLarge.Quantity.Should().BeGreaterThan(chickenSmall.Quantity);
        (chickenSmall.Quantity % 10).Should().Be(0, "grams are rounded to 10");
        small.TotalCalories.Should().Be(small.FoodItems.Sum(f => f.Calories));
    }

    [Fact]
    public void Every_meal_and_food_is_named_in_arabic_with_one_alternative_per_slot()
    {
        foreach (var diet in ReadyDietCatalog.All)
        {
            diet.DayB.Should().HaveCount(diet.MealsPerDay);
            diet.Alternatives.Should().HaveCount(diet.MealsPerDay);
            diet.Alternatives.Select(a => a.Type).Should().Equal(diet.DayA.Select(m => m.Type), "a swap replaces the same kind of meal");
            foreach (var meal in diet.DayA.Concat(diet.DayB).Concat(diet.Alternatives))
                meal.TitleAr.Should().MatchRegex("[؀-ۿ]");
        }
        ReadyDietCatalog.All.Select(d => d.Key).Should().OnlyHaveUniqueItems();
    }

    [Fact]
    public void Diets_for_the_goal_come_first()
    {
        ReadyDietCatalog.ForGoal(DietGoal.MuscleGain)[0].Key.Should().Be("muscle_gain_home");
        ReadyDietCatalog.ForGoal(DietGoal.FatLoss)[0].Key.Should().Be("fat_loss_high_protein");
        ReadyDietCatalog.Find("nope").Should().BeNull();
    }
}
