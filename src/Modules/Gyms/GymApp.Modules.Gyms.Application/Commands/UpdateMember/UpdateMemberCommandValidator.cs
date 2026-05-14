using FluentValidation;

namespace GymApp.Modules.Gyms.Application.Commands.UpdateMember;

internal sealed class UpdateMemberCommandValidator : AbstractValidator<UpdateMemberCommand>
{
    public UpdateMemberCommandValidator()
    {
        RuleFor(x => x.GymId).NotEmpty();
        RuleFor(x => x.MemberId).NotEmpty();
    }
}
