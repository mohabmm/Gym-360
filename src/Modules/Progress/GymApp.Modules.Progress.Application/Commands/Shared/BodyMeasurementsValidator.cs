using FluentValidation;
using GymApp.Modules.Progress.Application.DTOs;

namespace GymApp.Modules.Progress.Application.Commands.Shared;

/// <summary>Each tape measurement, when present, must be a plausible circumference (cm or inches).</summary>
internal sealed class BodyMeasurementsValidator : AbstractValidator<BodyMeasurementsDto>
{
    internal const decimal MinCm = 10;
    internal const decimal MaxCm = 250;
    internal const decimal MinIn = 4;
    internal const decimal MaxIn = 100;

    public BodyMeasurementsValidator()
    {
        Circumference(x => x.Chest, nameof(BodyMeasurementsDto.Chest));
        Circumference(x => x.Waist, nameof(BodyMeasurementsDto.Waist));
        Circumference(x => x.Hips, nameof(BodyMeasurementsDto.Hips));
        Circumference(x => x.LeftArm, nameof(BodyMeasurementsDto.LeftArm));
        Circumference(x => x.RightArm, nameof(BodyMeasurementsDto.RightArm));
        Circumference(x => x.LeftThigh, nameof(BodyMeasurementsDto.LeftThigh));
        Circumference(x => x.RightThigh, nameof(BodyMeasurementsDto.RightThigh));
        Circumference(x => x.LeftCalf, nameof(BodyMeasurementsDto.LeftCalf));
        Circumference(x => x.RightCalf, nameof(BodyMeasurementsDto.RightCalf));
        Circumference(x => x.Neck, nameof(BodyMeasurementsDto.Neck));
    }

    private void Circumference(Func<BodyMeasurementsDto, decimal?> value, string name)
    {
        RuleFor(x => value(x)!.Value)
            .InclusiveBetween(MinCm, MaxCm)
            .When(x => value(x).HasValue && !IsInches(x.MeasurementUnit))
            .OverridePropertyName(name);
        RuleFor(x => value(x)!.Value)
            .InclusiveBetween(MinIn, MaxIn)
            .When(x => value(x).HasValue && IsInches(x.MeasurementUnit))
            .OverridePropertyName(name);
    }

    private static bool IsInches(string? unit) =>
        unit is not null && unit.StartsWith("in", StringComparison.OrdinalIgnoreCase);
}
