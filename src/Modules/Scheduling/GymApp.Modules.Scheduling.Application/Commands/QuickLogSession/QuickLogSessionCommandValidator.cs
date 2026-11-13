using FluentValidation;

namespace GymApp.Modules.Scheduling.Application.Commands.QuickLogSession;

internal sealed class QuickLogSessionCommandValidator : AbstractValidator<QuickLogSessionCommand>
{
    public QuickLogSessionCommandValidator()
    {
        RuleFor(x => x.GymId).NotEmpty();
        RuleFor(x => x.TrainerId).NotEmpty();
        RuleFor(x => x.MemberId).NotEmpty();
        RuleFor(x => x.Notes).MaximumLength(500);
    }
}
