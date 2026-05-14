using FluentValidation;

namespace GymApp.Modules.Identity.Application.Commands.CreateMemberByStaff;

internal sealed class CreateMemberByStaffCommandValidator : AbstractValidator<CreateMemberByStaffCommand>
{
    public CreateMemberByStaffCommandValidator()
    {
        RuleFor(x => x.FirstName)
            .NotEmpty().WithMessage("First name is required.")
            .MaximumLength(100).WithMessage("First name cannot exceed 100 characters.");

        RuleFor(x => x.LastName)
            .NotEmpty().WithMessage("Last name is required.")
            .MaximumLength(100).WithMessage("Last name cannot exceed 100 characters.");

        RuleFor(x => x.Phone)
            .NotEmpty().WithMessage("Phone number is required.")
            .Must(p => p.Trim().Length >= 7).WithMessage("Phone number is too short.");

        RuleFor(x => x.Email)
            .EmailAddress().WithMessage("A valid email address is required.")
            .When(x => !string.IsNullOrWhiteSpace(x.Email));
    }
}
