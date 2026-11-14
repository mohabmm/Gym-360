using FluentValidation;

namespace GymApp.Modules.Identity.Application.Commands.AdminUsers;

internal sealed class SetAdminUserStatusCommandValidator : AbstractValidator<SetAdminUserStatusCommand>
{
    public SetAdminUserStatusCommandValidator()
    {
        RuleFor(x => x.UserId).NotEmpty();
        RuleFor(x => x.Status).NotEmpty()
            .Must(s => s is SetAdminUserStatusCommandHandler.Active or SetAdminUserStatusCommandHandler.Suspended)
            .WithMessage("Status must be 'active' or 'suspended'.");
    }
}
