using FluentValidation;

namespace GymApp.Modules.Gyms.Application.Commands.UnfreezeMember;

internal sealed class UnfreezeMemberCommandValidator : AbstractValidator<UnfreezeMemberCommand>
{
    public UnfreezeMemberCommandValidator()
    {
        RuleFor(x => x.GymId).NotEmpty();
        RuleFor(x => x.MemberId).NotEmpty();
    }
}
