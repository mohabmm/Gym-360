using FluentValidation;

namespace GymApp.Modules.Gyms.Application.Commands.AddTrainer;

internal sealed class AddTrainerCommandValidator : AbstractValidator<AddTrainerCommand>
{
    public AddTrainerCommandValidator()
    {
        RuleFor(x => x.GymId).NotEmpty();
        RuleFor(x => x.TrainerId).NotEmpty();
        RuleFor(x => x.Bio).MaximumLength(2000).When(x => x.Bio is not null);
        RuleForEach(x => x.Specializations).MaximumLength(100).When(x => x.Specializations is not null);
    }
}
