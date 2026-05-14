using FluentValidation;

namespace GymApp.Modules.Gyms.Application.Commands.FreezeMember;

internal sealed class FreezeMemberCommandValidator : AbstractValidator<FreezeMemberCommand>
{
    public FreezeMemberCommandValidator()
    {
        RuleFor(x => x.GymId).NotEmpty();
        RuleFor(x => x.MemberId).NotEmpty();
    }
}
