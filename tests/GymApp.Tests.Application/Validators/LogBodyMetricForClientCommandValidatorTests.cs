using FluentValidation.TestHelper;
using GymApp.Modules.Progress.Application.Commands.LogBodyMetricForClient;
using GymApp.Modules.Progress.Application.DTOs;

namespace GymApp.Tests.Application.Validators;

public class LogBodyMetricForClientCommandValidatorTests
{
    private readonly LogBodyMetricForClientCommandValidator _validator = new();

    private static DateOnly Today => DateOnly.FromDateTime(DateTime.UtcNow);

    private static BodyMeasurementsDto Tape(decimal? waist = 85, decimal? leftCalf = 38, string unit = "cm") =>
        new(Chest: 100, Waist: waist, Hips: 98, LeftArm: 35, RightArm: 35.5m, LeftThigh: 58, RightThigh: 58,
            LeftCalf: leftCalf, RightCalf: 38, Neck: 39, MeasurementUnit: unit);

    private static LogBodyMetricForClientCommand Valid() =>
        new(TrainerId: Guid.NewGuid(), ClientId: Guid.NewGuid(), GymId: Guid.NewGuid(), Date: Today,
            Weight: 82.4m, WeightUnit: "kg", BodyFatPercentage: 18, Measurements: Tape(), Notes: "Morning, fasted");

    [Fact]
    public void Validate_RealisticReading_Passes() =>
        _validator.TestValidate(Valid()).ShouldNotHaveAnyValidationErrors();

    [Fact]
    public void Validate_OnlyWeight_Passes() =>
        _validator.TestValidate(Valid() with { BodyFatPercentage = null, Measurements = null, Notes = null })
            .ShouldNotHaveAnyValidationErrors();

    [Theory]
    [InlineData(19.9)]
    [InlineData(350.1)]
    [InlineData(820)]
    public void Validate_WeightOutsideHumanRangeInKg_Fails(decimal weight) =>
        _validator.TestValidate(Valid() with { Weight = weight })
            .ShouldHaveValidationErrorFor(nameof(LogBodyMetricForClientCommand.Weight));

    [Fact]
    public void Validate_WeightWithoutUnit_UsesKgRange() =>
        _validator.TestValidate(Valid() with { Weight = 400, WeightUnit = null })
            .ShouldHaveValidationErrorFor(nameof(LogBodyMetricForClientCommand.Weight));

    [Theory]
    [InlineData(400, false)]
    [InlineData(800, true)]
    [InlineData(40, true)]
    public void Validate_WeightInPounds_UsesPoundRange(decimal weight, bool fails)
    {
        var result = _validator.TestValidate(Valid() with { Weight = weight, WeightUnit = "lbs" });
        if (fails) result.ShouldHaveValidationErrorFor(nameof(LogBodyMetricForClientCommand.Weight));
        else result.ShouldNotHaveValidationErrorFor(nameof(LogBodyMetricForClientCommand.Weight));
    }

    [Theory]
    [InlineData(1.9)]
    [InlineData(75.1)]
    public void Validate_BodyFatOutsideRange_Fails(decimal bodyFat) =>
        _validator.TestValidate(Valid() with { BodyFatPercentage = bodyFat })
            .ShouldHaveValidationErrorFor(nameof(LogBodyMetricForClientCommand.BodyFatPercentage));

    [Theory]
    [InlineData(9)]
    [InlineData(251)]
    public void Validate_WaistOutsideRange_Fails(decimal waist) =>
        _validator.TestValidate(Valid() with { Measurements = Tape(waist: waist) })
            .ShouldHaveValidationErrorFor("Measurements.Waist");

    [Fact]
    public void Validate_CalfOutsideRange_Fails() =>
        _validator.TestValidate(Valid() with { Measurements = Tape(leftCalf: 380) })
            .ShouldHaveValidationErrorFor("Measurements.LeftCalf");

    [Fact]
    public void Validate_MissingTapeValues_AreSkipped() =>
        _validator.TestValidate(Valid() with { Measurements = Tape(waist: null, leftCalf: null) })
            .ShouldNotHaveAnyValidationErrors();

    [Fact]
    public void Validate_InchesUseInchRange()
    {
        var inches = Tape(waist: 34, leftCalf: 15, unit: "in") with
        {
            Chest = 40, Hips = 38, LeftArm = 14, RightArm = 14, LeftThigh = 23, RightThigh = 23, RightCalf = 15, Neck = 15
        };
        _validator.TestValidate(Valid() with { Measurements = inches }).ShouldNotHaveAnyValidationErrors();
        _validator.TestValidate(Valid() with { Measurements = inches with { Waist = 120 } })
            .ShouldHaveValidationErrorFor("Measurements.Waist");
    }

    [Fact]
    public void Validate_DateInTheFuture_Fails() =>
        _validator.TestValidate(Valid() with { Date = Today.AddDays(2) })
            .ShouldHaveValidationErrorFor(x => x.Date);

    [Fact]
    public void Validate_TomorrowUtc_PassesForClientsAheadOfUtc() =>
        _validator.TestValidate(Valid() with { Date = Today.AddDays(1) })
            .ShouldNotHaveValidationErrorFor(x => x.Date);

    [Fact]
    public void Validate_DateOlderThanAYear_Fails() =>
        _validator.TestValidate(Valid() with { Date = Today.AddDays(-366) })
            .ShouldHaveValidationErrorFor(x => x.Date);

    [Fact]
    public void Validate_BackdatedWithinAYear_Passes() =>
        _validator.TestValidate(Valid() with { Date = Today.AddDays(-30) })
            .ShouldNotHaveValidationErrorFor(x => x.Date);

    [Fact]
    public void Validate_NotesTooLong_Fails() =>
        _validator.TestValidate(Valid() with { Notes = new string('a', 501) })
            .ShouldHaveValidationErrorFor(x => x.Notes);

    [Fact]
    public void Validate_EmptyClientId_Fails() =>
        _validator.TestValidate(Valid() with { ClientId = Guid.Empty })
            .ShouldHaveValidationErrorFor(x => x.ClientId);
}
