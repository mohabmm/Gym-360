using FluentValidation;
using GymApp.Modules.Progress.Application.DTOs;

namespace GymApp.Modules.Progress.Application.Commands.Shared;

/// <summary>
/// Rejects readings no human body has (typos like 820 kg or a 9 cm waist). The bounds are wider than
/// the apps' per-field ranges so the apps are always the stricter side.
/// </summary>
internal sealed class BodyMetricReadingValidator : AbstractValidator<IBodyMetricReading>
{
    internal const decimal MinWeightKg = 20;
    internal const decimal MaxWeightKg = 350;
    internal const decimal MinWeightLbs = 44;
    internal const decimal MaxWeightLbs = 772;
    internal const decimal MinBodyFat = 2;
    internal const decimal MaxBodyFat = 75;
    internal const int MaxNotesLength = 500;
    internal const int MaxDaysBack = 365;

    public BodyMetricReadingValidator()
    {
        RuleFor(x => x.Date)
            .NotEmpty()
            .Must(BeWithinRecordableWindow)
            .WithMessage($"Date must be today or within the last {MaxDaysBack} days.");

        RuleFor(x => x.Weight!.Value)
            .InclusiveBetween(MinWeightKg, MaxWeightKg)
            .When(x => x.Weight.HasValue && !IsPounds(x.WeightUnit))
            .OverridePropertyName(nameof(IBodyMetricReading.Weight));
        RuleFor(x => x.Weight!.Value)
            .InclusiveBetween(MinWeightLbs, MaxWeightLbs)
            .When(x => x.Weight.HasValue && IsPounds(x.WeightUnit))
            .OverridePropertyName(nameof(IBodyMetricReading.Weight));

        RuleFor(x => x.BodyFatPercentage!.Value)
            .InclusiveBetween(MinBodyFat, MaxBodyFat)
            .When(x => x.BodyFatPercentage.HasValue)
            .OverridePropertyName(nameof(IBodyMetricReading.BodyFatPercentage));

        RuleFor(x => x.Measurements!)
            .SetValidator(new BodyMeasurementsValidator())
            .When(x => x.Measurements is not null);

        RuleFor(x => x.Notes)
            .MaximumLength(MaxNotesLength)
            .When(x => x.Notes is not null);
    }

    private static bool IsPounds(string? unit) =>
        unit is not null && unit.StartsWith("lb", StringComparison.OrdinalIgnoreCase);

    // One day of slack for clients ahead of UTC (Egypt is UTC+2/+3).
    private static bool BeWithinRecordableWindow(DateOnly date)
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        return date <= today.AddDays(1) && date >= today.AddDays(-MaxDaysBack);
    }
}
