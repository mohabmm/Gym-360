using FluentValidation;
using GymApp.Modules.Training.Domain.Enums;

namespace GymApp.Modules.Training.Application.Commands.CreateExercise;

internal sealed class CreateExerciseCommandValidator : AbstractValidator<CreateExerciseCommand>
{
    public CreateExerciseCommandValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Exercise name is required.")
            .MaximumLength(200);

        RuleFor(x => x.MuscleGroups)
            .NotEmpty().WithMessage("At least one muscle group is required.");

        // Handler calls Enum.Parse<MuscleGroup> without a safety net, so an unknown
        // value propagates as ArgumentException → 500. Validate each entry here so
        // unknown values surface as a clean 400 VALIDATION_ERROR instead.
        RuleForEach(x => x.MuscleGroups)
            .Must(mg => Enum.TryParse<MuscleGroup>(mg, ignoreCase: true, out _))
            .WithMessage("Invalid muscle group value: '{PropertyValue}'.");

        RuleFor(x => x.Equipment)
            .NotEmpty()
            .Must(e => Enum.TryParse<Equipment>(e, ignoreCase: true, out _))
            .WithMessage("Invalid equipment value.");

        RuleFor(x => x.Category)
            .NotEmpty()
            .Must(c => Enum.TryParse<ExerciseCategory>(c, ignoreCase: true, out _))
            .WithMessage("Invalid category value.");

        RuleFor(x => x.Scope)
            .NotEmpty()
            .Must(s => Enum.TryParse<ExerciseScope>(s, ignoreCase: true, out _))
            .WithMessage("Invalid scope value.");

        RuleFor(x => x.GymId)
            .NotNull()
            .When(x => string.Equals(x.Scope, "GymSpecific", StringComparison.OrdinalIgnoreCase))
            .WithMessage("GymId is required for gym-specific exercises.");

        RuleFor(x => x.Description).MaximumLength(1000);
        RuleFor(x => x.Instructions).MaximumLength(2000);
        RuleFor(x => x.DemoMediaUrl).MaximumLength(500);
    }
}
