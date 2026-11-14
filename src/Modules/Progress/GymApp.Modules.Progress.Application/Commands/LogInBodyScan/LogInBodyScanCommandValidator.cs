using FluentValidation;
using GymApp.Modules.Progress.Domain.ValueObjects;

namespace GymApp.Modules.Progress.Application.Commands.LogInBodyScan;

internal sealed class LogInBodyScanCommandValidator : AbstractValidator<LogInBodyScanCommand>
{
    public LogInBodyScanCommandValidator()
    {
        RuleFor(x => x.UserId).NotEmpty();
        RuleFor(x => x.GymId).NotEmpty();
        RuleFor(x => x.Date).NotEmpty();
        RuleFor(x => x.Notes).MaximumLength(500);
        RuleFor(x => x.Values)
            .NotNull()
            .Must(HasAnyValue)
            .WithMessage("An InBody scan needs at least one value.");
        RuleFor(x => x.Values).SetValidator(new ValuesValidator()).When(x => x.Values is not null);
    }

    private static bool HasAnyValue(InBodyScanValues? v) =>
        v is not null
        && (v.WeightKg ?? v.HeightCm ?? v.BodyFatPercentage ?? v.SkeletalMuscleMassKg ?? v.BodyWaterKg) is not null;

    private sealed class ValuesValidator : AbstractValidator<InBodyScanValues>
    {
        public ValuesValidator()
        {
            RuleFor(v => v.WeightKg).GreaterThan(0).When(v => v.WeightKg is not null);
            RuleFor(v => v.BodyFatPercentage).InclusiveBetween(0, 100).When(v => v.BodyFatPercentage is not null);
            RuleFor(v => v.HeightCm)
                .InclusiveBetween(BodyComposition.MinHeightCm, BodyComposition.MaxHeightCm)
                .When(v => v.HeightCm is not null);
            RuleFor(v => v.SkeletalMuscleMassKg).GreaterThan(0).When(v => v.SkeletalMuscleMassKg is not null);
            RuleFor(v => v.BodyWaterKg).GreaterThan(0).When(v => v.BodyWaterKg is not null);
        }
    }
}
