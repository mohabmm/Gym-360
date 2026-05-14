using FluentValidation;

namespace GymApp.Modules.Gamification.Application.Commands.SubmitTrainerRating;

internal sealed class SubmitTrainerRatingCommandValidator : AbstractValidator<SubmitTrainerRatingCommand>
{
    public SubmitTrainerRatingCommandValidator()
    {
        RuleFor(x => x.GymId).NotEmpty().WithMessage("Gym ID is required.");
        RuleFor(x => x.TrainerId).NotEmpty().WithMessage("Trainer ID is required.");
        RuleFor(x => x.ClientId).NotEmpty().WithMessage("Client ID is required.");
        RuleFor(x => x.SessionId).NotEmpty().WithMessage("Session ID is required.");
        RuleFor(x => x.Stars).InclusiveBetween(1, 5).WithMessage("Stars must be between 1 and 5.");
        RuleFor(x => x.Comment).MaximumLength(1000).WithMessage("Comment must not exceed 1000 characters.");
    }
}
