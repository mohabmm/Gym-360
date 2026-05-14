using FluentValidation;

namespace GymApp.Modules.Progress.Application.Commands.LogBodyMetric;

internal sealed class LogBodyMetricCommandValidator : AbstractValidator<LogBodyMetricCommand>
{
    public LogBodyMetricCommandValidator()
    {
        RuleFor(x => x.UserId).NotEmpty();
        RuleFor(x => x.GymId).NotEmpty();
        RuleFor(x => x.Date).NotEmpty();
        RuleFor(x => x.LoggedBy).NotEmpty();
        RuleFor(x => x.Source).NotEmpty();
        RuleFor(x => x.Weight)
            .GreaterThan(0)
            .When(x => x.Weight.HasValue);
        RuleFor(x => x.BodyFatPercentage)
            .InclusiveBetween(0, 100)
            .When(x => x.BodyFatPercentage.HasValue);
    }
}
