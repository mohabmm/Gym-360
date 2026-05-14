using FluentValidation;

namespace GymApp.Modules.Gamification.Application.Commands.CalculateMonthlyTrainerScores;

internal sealed class CalculateMonthlyTrainerScoresCommandValidator : AbstractValidator<CalculateMonthlyTrainerScoresCommand>
{
    public CalculateMonthlyTrainerScoresCommandValidator()
    {
        RuleFor(x => x.GymId).NotEmpty().WithMessage("Gym ID is required.");
        RuleFor(x => x.Month).InclusiveBetween(1, 12).WithMessage("Month must be between 1 and 12.");
        RuleFor(x => x.Year).InclusiveBetween(2000, 2100).WithMessage("Year must be between 2000 and 2100.");
    }
}
