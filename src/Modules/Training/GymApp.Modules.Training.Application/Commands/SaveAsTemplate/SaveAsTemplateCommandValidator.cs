using FluentValidation;

namespace GymApp.Modules.Training.Application.Commands.SaveAsTemplate;

internal sealed class SaveAsTemplateCommandValidator : AbstractValidator<SaveAsTemplateCommand>
{
    public SaveAsTemplateCommandValidator()
    {
        RuleFor(x => x.WorkoutId).NotEmpty();
    }
}
