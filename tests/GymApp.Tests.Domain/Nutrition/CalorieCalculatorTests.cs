using GymApp.Modules.Nutrition.Domain.Diet;

namespace GymApp.Tests.Domain.Nutrition;

public class CalorieCalculatorTests
{
    [Fact]
    public void Man_losing_fat_gets_mifflin_maintenance_minus_twenty_percent_with_high_protein()
    {
        // BMR = 10×80 + 6.25×180 − 5×30 + 5 = 1780; × 1.55 = 2759; × 0.8 = 2207.
        var target = CalorieCalculator.Calculate(new BodyProfile(Sex.Male, 30, 180, 80, ActivityLevel.Moderate, DietGoal.FatLoss)).Value;

        target.Bmr.Should().Be(1780);
        target.Maintenance.Should().Be(2760);
        target.Calories.Should().Be(2210);
        target.ProteinGrams.Should().Be(160, "2 g per kg while losing fat");
        target.FatGrams.Should().Be(60, "a quarter of the calories");
        target.CarbsGrams.Should().Be(260, "the rest");
    }

    [Fact]
    public void Never_goes_below_a_safe_floor()
    {
        // BMR = 500 + 1000 − 125 − 161 = 1214; × 1.2 × 0.8 = 1165 → floor 1200.
        var target = CalorieCalculator.Calculate(new BodyProfile(Sex.Female, 25, 160, 50, ActivityLevel.Low, DietGoal.FatLoss)).Value;

        target.Calories.Should().Be(1200);
    }

    [Fact]
    public void Muscle_gain_adds_ten_percent()
    {
        var body = new BodyProfile(Sex.Male, 22, 175, 70, ActivityLevel.High, DietGoal.Maintain);
        var maintain = CalorieCalculator.Calculate(body).Value;
        var gain = CalorieCalculator.Calculate(body with { Goal = DietGoal.MuscleGain }).Value;

        gain.Calories.Should().BeCloseTo((int)(maintain.Calories * 1.1m), 10);
        gain.ProteinGrams.Should().Be(125, "1.8 g per kg");
    }

    [Theory]
    [InlineData(15, 170, 70)]
    [InlineData(30, 100, 70)]
    [InlineData(30, 170, 300)]
    public void Refuses_values_outside_the_safe_range(int age, int height, int weight)
    {
        var result = CalorieCalculator.Calculate(new BodyProfile(Sex.Male, age, height, weight, ActivityLevel.Light, DietGoal.Maintain));

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Nutrition.Diet.InvalidBody");
    }
}
