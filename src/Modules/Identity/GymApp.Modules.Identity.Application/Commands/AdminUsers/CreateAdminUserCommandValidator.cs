using FluentValidation;

namespace GymApp.Modules.Identity.Application.Commands.AdminUsers;

internal sealed class CreateAdminUserCommandValidator : AbstractValidator<CreateAdminUserCommand>
{
    public CreateAdminUserCommandValidator()
    {
        RuleFor(x => x.Email).NotEmpty().EmailAddress();
        RuleFor(x => x.Password).NotEmpty().MinimumLength(8);
        RuleFor(x => x.FirstName).NotEmpty().MaximumLength(100);
        RuleFor(x => x.LastName).NotEmpty().MaximumLength(100);
        RuleFor(x => x.AdminRole).NotEmpty()
            .Must(r => r is "super_admin" or "admin" or "support" or "analyst")
            .WithMessage("AdminRole must be one of: super_admin, admin, support, analyst");
    }
}
