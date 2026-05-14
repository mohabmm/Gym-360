using FluentValidation;

namespace GymApp.Modules.Gamification.Application.Commands.RecalculateLeaderboards;

internal sealed class RecalculateLeaderboardsCommandValidator : AbstractValidator<RecalculateLeaderboardsCommand>
{
    public RecalculateLeaderboardsCommandValidator()
    {
        RuleFor(x => x.GymId).NotEmpty().WithMessage("Gym ID is required.");
    }
}
