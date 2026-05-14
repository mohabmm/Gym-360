using FluentValidation;

namespace GymApp.Modules.Identity.Application.Commands.AdminUsers;

internal sealed class CreateGymStaffCommandValidator : AbstractValidator<CreateGymStaffCommand>
{
    public CreateGymStaffCommandValidator()
    {
        RuleFor(x => x.GymId).NotEmpty();
        RuleFor(x => x.Email).NotEmpty().EmailAddress();
        RuleFor(x => x.Password).NotEmpty().MinimumLength(8);
        RuleFor(x => x.FirstName).NotEmpty().MaximumLength(100);
        RuleFor(x => x.LastName).NotEmpty().MaximumLength(100);
    }
}
