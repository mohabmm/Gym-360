using FluentValidation;

namespace GymApp.Modules.Gamification.Application.Commands.ToggleLeaderboardOptOut;

internal sealed class ToggleLeaderboardOptOutCommandValidator : AbstractValidator<ToggleLeaderboardOptOutCommand>
{
    public ToggleLeaderboardOptOutCommandValidator()
    {
        RuleFor(x => x.UserId).NotEmpty().WithMessage("User ID is required.");
        RuleFor(x => x.GymId).NotEmpty().WithMessage("Gym ID is required.");
    }
}
