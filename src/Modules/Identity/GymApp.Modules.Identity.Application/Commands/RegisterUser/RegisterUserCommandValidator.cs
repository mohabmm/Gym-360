using FluentValidation;

namespace GymApp.Modules.Identity.Application.Commands.RegisterUser;

internal sealed class RegisterUserCommandValidator : AbstractValidator<RegisterUserCommand>
{
    public RegisterUserCommandValidator()
    {
        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("Email is required.")
            .EmailAddress().WithMessage("A valid email address is required.");

        RuleFor(x => x.Password)
            .NotEmpty().WithMessage("Password is required.")
            .MinimumLength(8).WithMessage("Password must be at least 8 characters.");

        RuleFor(x => x.FirstName)
            .NotEmpty().WithMessage("First name is required.")
            .MaximumLength(100).WithMessage("First name cannot exceed 100 characters.");

        RuleFor(x => x.LastName)
            .NotEmpty().WithMessage("Last name is required.")
            .MaximumLength(100).WithMessage("Last name cannot exceed 100 characters.");

        // Phone is optional. The handler normalizes Egyptian local formats
        // (e.g. "01128506941" -> "+201128506941") and substitutes a placeholder
        // when empty so the storage NOT NULL constraint is satisfied.
        RuleFor(x => x.Phone)
            .Must(p => string.IsNullOrWhiteSpace(p) || p.Trim().Length >= 7)
            .WithMessage("Phone number is too short.");

        RuleFor(x => x.Role)
            .NotEmpty().WithMessage("Role is required.");
    }
}
