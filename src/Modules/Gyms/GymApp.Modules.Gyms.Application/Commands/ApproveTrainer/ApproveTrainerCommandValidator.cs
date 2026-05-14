using FluentValidation;

namespace GymApp.Modules.Gyms.Application.Commands.ApproveTrainer;

internal sealed class ApproveTrainerCommandValidator : AbstractValidator<ApproveTrainerCommand>
{
    public ApproveTrainerCommandValidator()
    {
        RuleFor(x => x.GymId).NotEmpty();
        RuleFor(x => x.TrainerId).NotEmpty();
    }
}
