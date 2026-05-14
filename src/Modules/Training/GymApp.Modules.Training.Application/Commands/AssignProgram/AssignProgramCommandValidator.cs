using FluentValidation;

namespace GymApp.Modules.Training.Application.Commands.AssignProgram;

internal sealed class AssignProgramCommandValidator : AbstractValidator<AssignProgramCommand>
{
    public AssignProgramCommandValidator()
    {
        RuleFor(x => x.TrainerId).NotEmpty();
        RuleFor(x => x.ClientId).NotEmpty();
        RuleFor(x => x.GymId).NotEmpty();
        RuleFor(x => x.ProgramId).NotEmpty();
        RuleFor(x => x.StartDate).NotEmpty();
        RuleFor(x => x.Notes).MaximumLength(1000);
    }
}
