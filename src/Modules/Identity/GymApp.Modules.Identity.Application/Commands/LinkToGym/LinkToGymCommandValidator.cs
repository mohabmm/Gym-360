using FluentValidation;

namespace GymApp.Modules.Identity.Application.Commands.LinkToGym;

internal sealed class LinkToGymCommandValidator : AbstractValidator<LinkToGymCommand>
{
    public LinkToGymCommandValidator()
    {
        RuleFor(x => x.UserId)
            .NotEmpty().WithMessage("User ID is required.");

        RuleFor(x => x.GymCode)
            .NotEmpty().WithMessage("Gym code is required.")
            .MaximumLength(20).WithMessage("Gym code cannot exceed 20 characters.");
    }
}
