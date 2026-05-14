using FluentValidation;

namespace GymApp.Modules.Scheduling.Application.Commands.UnblockDate;

internal sealed class UnblockDateCommandValidator : AbstractValidator<UnblockDateCommand>
{
    public UnblockDateCommandValidator()
    {
        RuleFor(x => x.TrainerId).NotEmpty().WithMessage("Trainer ID is required.");
        RuleFor(x => x.GymId).NotEmpty().WithMessage("Gym ID is required.");
    }
}
