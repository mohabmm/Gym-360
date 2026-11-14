using FluentValidation.TestHelper;
using GymApp.Modules.Progress.Application.Commands.LogBodyMetric;
using GymApp.Modules.Progress.Application.DTOs;

namespace GymApp.Tests.Application.Validators;

/// <summary>The member's self-logged reading uses the same plausibility rules as the coach's.</summary>
public class LogBodyMetricCommandValidatorTests
{
    private readonly LogBodyMetricCommandValidator _validator = new();

    private static LogBodyMetricCommand Valid()
    {
        var userId = Guid.NewGuid();
        return new(UserId: userId, GymId: Guid.NewGuid(), Date: DateOnly.FromDateTime(DateTime.UtcNow),
            Weight: 70, WeightUnit: "kg", BodyFatPercentage: null,
            Measurements: new BodyMeasurementsDto(null, 80, null, 32, 32, null, null, 36, 36, 37, "cm"),
            Source: "SelfReported", LoggedBy: userId, Notes: null);
    }

    [Fact]
    public void Validate_RealisticSelfLoggedReading_Passes() =>
        _validator.TestValidate(Valid()).ShouldNotHaveAnyValidationErrors();

    [Fact]
    public void Validate_TypoWeight_Fails() =>
        _validator.TestValidate(Valid() with { Weight = 700 })
            .ShouldHaveValidationErrorFor(nameof(LogBodyMetricCommand.Weight));

    [Fact]
    public void Validate_TypoNeck_Fails() =>
        _validator.TestValidate(Valid() with { Measurements = Valid().Measurements! with { Neck = 3.7m } })
            .ShouldHaveValidationErrorFor("Measurements.Neck");

    [Fact]
    public void Validate_FutureDate_Fails() =>
        _validator.TestValidate(Valid() with { Date = DateOnly.FromDateTime(DateTime.UtcNow).AddDays(3) })
            .ShouldHaveValidationErrorFor(x => x.Date);

    [Fact]
    public void Validate_MissingSource_Fails() =>
        _validator.TestValidate(Valid() with { Source = "" })
            .ShouldHaveValidationErrorFor(x => x.Source);
}
