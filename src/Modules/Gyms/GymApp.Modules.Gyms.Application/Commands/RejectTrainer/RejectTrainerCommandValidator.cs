using FluentValidation;

namespace GymApp.Modules.Gyms.Application.Commands.RejectTrainer;

internal sealed class RejectTrainerCommandValidator : AbstractValidator<RejectTrainerCommand>
{
    public RejectTrainerCommandValidator()
    {
        RuleFor(x => x.GymId).NotEmpty();
        RuleFor(x => x.TrainerId).NotEmpty();
    }
}
