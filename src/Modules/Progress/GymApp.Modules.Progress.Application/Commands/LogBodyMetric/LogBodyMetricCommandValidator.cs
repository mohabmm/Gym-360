using FluentValidation;
using GymApp.Modules.Progress.Application.Commands.Shared;

namespace GymApp.Modules.Progress.Application.Commands.LogBodyMetric;

internal sealed class LogBodyMetricCommandValidator : AbstractValidator<LogBodyMetricCommand>
{
    public LogBodyMetricCommandValidator()
    {
        RuleFor(x => x.UserId).NotEmpty();
        RuleFor(x => x.GymId).NotEmpty();
        RuleFor(x => x.LoggedBy).NotEmpty();
        RuleFor(x => x.Source).NotEmpty();
        Include(new BodyMetricReadingValidator());
    }
}
