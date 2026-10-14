using GymApp.Modules.Scheduling.Application.Abstractions;
using FluentValidation;

namespace GymApp.Modules.Scheduling.Application.Commands.BookSession;

internal sealed class BookSessionCommandValidator : AbstractValidator<BookSessionCommand>
{
    public BookSessionCommandValidator()
    {
        RuleFor(x => x.GymId)
            .NotEmpty().WithMessage("Gym ID is required.");

        RuleFor(x => x.TrainerId)
            .NotEmpty().WithMessage("Trainer ID is required.");

        RuleFor(x => x.MemberId)
            .NotEmpty().WithMessage("Member ID is required.");

        RuleFor(x => x.Date)
            .Must(d => d >= GymLocalTime.Today())
            .WithMessage("Cannot book a session in the past.");

        RuleFor(x => x.DurationMinutes)
            .InclusiveBetween(15, 180).WithMessage("Duration must be between 15 and 180 minutes.");

        RuleFor(x => x.SessionType)
            .NotEmpty().WithMessage("Session type is required.");
    }
}
