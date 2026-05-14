using FluentValidation;

namespace GymApp.Modules.Identity.Application.Commands.UpdateProfile;

internal sealed class UpdateProfileCommandValidator : AbstractValidator<UpdateProfileCommand>
{
    public UpdateProfileCommandValidator()
    {
        RuleFor(x => x.UserId)
            .NotEmpty().WithMessage("User ID is required.");

        RuleFor(x => x.FirstName)
            .MaximumLength(100).WithMessage("First name cannot exceed 100 characters.")
            .When(x => x.FirstName is not null);

        RuleFor(x => x.LastName)
            .MaximumLength(100).WithMessage("Last name cannot exceed 100 characters.")
            .When(x => x.LastName is not null);

        RuleFor(x => x.ProfilePhotoUrl)
            .MaximumLength(500).WithMessage("Profile photo URL cannot exceed 500 characters.")
            .When(x => x.ProfilePhotoUrl is not null);
    }
}
