using FluentValidation;

namespace GymApp.Modules.Gyms.Application.Commands.RemoveMember;

internal sealed class RemoveMemberCommandValidator : AbstractValidator<RemoveMemberCommand>
{
    public RemoveMemberCommandValidator()
    {
        RuleFor(x => x.GymId).NotEmpty();
        RuleFor(x => x.MemberId).NotEmpty();
    }
}
