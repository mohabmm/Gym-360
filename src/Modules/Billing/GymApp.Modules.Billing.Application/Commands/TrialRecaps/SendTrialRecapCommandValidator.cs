using FluentValidation;
using GymApp.Modules.Billing.Domain.ValueObjects;

namespace GymApp.Modules.Billing.Application.Commands.TrialRecaps;

internal sealed class SendTrialRecapCommandValidator : AbstractValidator<SendTrialRecapCommand>
{
    public SendTrialRecapCommandValidator()
    {
        RuleFor(x => x.GymId).NotEmpty();
        RuleFor(x => x.PackageId).NotEmpty();
        RuleFor(x => x.TrainerId).NotEmpty();
        RuleFor(x => x.Note).NotEmpty().MaximumLength(TrialRecap.MaxNoteLength);
        RuleFor(x => x.RecommendedSessions)
            .Must(TrialRecap.IsValidSize)
            .WithMessage("Recommended sessions must be a multiple of 4 between 4 and 40.");
    }
}
