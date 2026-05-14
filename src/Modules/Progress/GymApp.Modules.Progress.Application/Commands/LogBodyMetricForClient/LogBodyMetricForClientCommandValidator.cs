using FluentValidation;

namespace GymApp.Modules.Progress.Application.Commands.LogBodyMetricForClient;

internal sealed class LogBodyMetricForClientCommandValidator : AbstractValidator<LogBodyMetricForClientCommand>
{
    public LogBodyMetricForClientCommandValidator()
    {
        RuleFor(x => x.TrainerId).NotEmpty();
        RuleFor(x => x.ClientId).NotEmpty();
        RuleFor(x => x.GymId).NotEmpty();
        RuleFor(x => x.Date).NotEmpty();
        RuleFor(x => x.Weight)
            .GreaterThan(0)
            .When(x => x.Weight.HasValue);
        RuleFor(x => x.BodyFatPercentage)
            .InclusiveBetween(0, 100)
            .When(x => x.BodyFatPercentage.HasValue);
    }
}
