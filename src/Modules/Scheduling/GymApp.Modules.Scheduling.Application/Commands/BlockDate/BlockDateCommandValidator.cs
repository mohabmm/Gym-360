using FluentValidation;

namespace GymApp.Modules.Scheduling.Application.Commands.BlockDate;

internal sealed class BlockDateCommandValidator : AbstractValidator<BlockDateCommand>
{
    public BlockDateCommandValidator()
    {
        RuleFor(x => x.TrainerId)
            .NotEmpty().WithMessage("Trainer ID is required.");

        RuleFor(x => x.GymId)
            .NotEmpty().WithMessage("Gym ID is required.");

        RuleFor(x => x.Date)
            .Must(d => d >= DateOnly.FromDateTime(DateTime.UtcNow.Date))
            .WithMessage("Cannot block a date in the past.");
    }
}
