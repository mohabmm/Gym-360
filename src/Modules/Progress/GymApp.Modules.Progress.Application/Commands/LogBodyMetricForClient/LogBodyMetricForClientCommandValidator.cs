using FluentValidation;
using GymApp.Modules.Progress.Application.Commands.Shared;

namespace GymApp.Modules.Progress.Application.Commands.LogBodyMetricForClient;

internal sealed class LogBodyMetricForClientCommandValidator : AbstractValidator<LogBodyMetricForClientCommand>
{
    public LogBodyMetricForClientCommandValidator()
    {
        RuleFor(x => x.TrainerId).NotEmpty();
        RuleFor(x => x.ClientId).NotEmpty();
        RuleFor(x => x.GymId).NotEmpty();
        Include(new BodyMetricReadingValidator());
    }
}
