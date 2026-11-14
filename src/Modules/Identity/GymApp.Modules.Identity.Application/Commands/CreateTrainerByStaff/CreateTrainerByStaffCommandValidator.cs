using FluentValidation;

namespace GymApp.Modules.Identity.Application.Commands.CreateTrainerByStaff;

internal sealed class CreateTrainerByStaffCommandValidator : AbstractValidator<CreateTrainerByStaffCommand>
{
    public CreateTrainerByStaffCommandValidator()
    {
        RuleFor(x => x.GymId)
            .NotEqual(Guid.Empty).WithMessage("GymId is required.");

        RuleFor(x => x.FirstName)
            .NotEmpty().WithMessage("First name is required.")
            .MaximumLength(100).WithMessage("First name cannot exceed 100 characters.");

        RuleFor(x => x.LastName)
            .NotEmpty().WithMessage("Last name is required.")
            .MaximumLength(100).WithMessage("Last name cannot exceed 100 characters.");

        RuleFor(x => x.Phone)
            .NotEmpty().WithMessage("Phone number is required.")
            .Must(p => p.Trim().Length >= 7).WithMessage("Phone number is too short.");

        // Trainers sign in to the Trainer app with their email, so it is required.
        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("Email is required: the trainer signs in with it.")
            .EmailAddress().WithMessage("A valid email address is required.");
    }
}
