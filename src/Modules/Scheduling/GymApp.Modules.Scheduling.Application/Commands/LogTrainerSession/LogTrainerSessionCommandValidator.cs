using FluentValidation;

namespace GymApp.Modules.Scheduling.Application.Commands.LogTrainerSession;

internal sealed class LogTrainerSessionCommandValidator : AbstractValidator<LogTrainerSessionCommand>
{
    public LogTrainerSessionCommandValidator()
    {
        RuleFor(x => x.SessionId)
            .NotEmpty().WithMessage("Session ID is required.");

        RuleFor(x => x.TrainerId)
            .NotEmpty().WithMessage("Trainer ID is required.");

        RuleFor(x => x.ExercisesPerformed)
            .NotEmpty().WithMessage("Exercises performed is required.")
            .MaximumLength(2000).WithMessage("Exercises performed cannot exceed 2000 characters.");

        RuleFor(x => x.ClientEffortRating)
            .InclusiveBetween(1, 5).WithMessage("Client effort rating must be between 1 and 5.");

        RuleFor(x => x.Feedback)
            .MaximumLength(1000).WithMessage("Feedback cannot exceed 1000 characters.");

        RuleFor(x => x.TechniqueNotes)
            .MaximumLength(1000).WithMessage("Technique notes cannot exceed 1000 characters.");

        RuleFor(x => x.AreasToImprove)
            .MaximumLength(1000).WithMessage("Areas to improve cannot exceed 1000 characters.");

        RuleFor(x => x.NextSessionFocus)
            .MaximumLength(500).WithMessage("Next session focus cannot exceed 500 characters.");
    }
}
