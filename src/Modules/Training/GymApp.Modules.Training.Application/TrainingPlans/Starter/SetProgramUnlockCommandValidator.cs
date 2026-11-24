using FluentValidation;

namespace GymApp.Modules.Training.Application.TrainingPlans.Starter;

internal sealed class SetProgramUnlockCommandValidator : AbstractValidator<SetProgramUnlockCommand>
{
    public SetProgramUnlockCommandValidator()
    {
        RuleFor(c => c.Pair.GymId).NotEmpty();
        RuleFor(c => c.Pair.TrainerId).NotEmpty();
        RuleFor(c => c.Pair.TraineeId).NotEmpty();
        RuleFor(c => c.StaffUserId).NotEmpty();
        RuleFor(c => c.Length).IsInEnum();
    }
}
