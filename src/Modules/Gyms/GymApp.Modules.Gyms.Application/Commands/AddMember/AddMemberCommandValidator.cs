using FluentValidation;

namespace GymApp.Modules.Gyms.Application.Commands.AddMember;

internal sealed class AddMemberCommandValidator : AbstractValidator<AddMemberCommand>
{
    public AddMemberCommandValidator()
    {
        RuleFor(x => x.GymId).NotEmpty();
        RuleFor(x => x.UserId).NotEmpty();
        RuleFor(x => x.MemberNumber).MaximumLength(50).When(x => x.MemberNumber is not null);
    }
}
