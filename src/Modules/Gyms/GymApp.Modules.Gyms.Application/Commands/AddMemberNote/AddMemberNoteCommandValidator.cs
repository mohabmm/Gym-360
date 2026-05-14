using FluentValidation;

namespace GymApp.Modules.Gyms.Application.Commands.AddMemberNote;

internal sealed class AddMemberNoteCommandValidator : AbstractValidator<AddMemberNoteCommand>
{
    public AddMemberNoteCommandValidator()
    {
        RuleFor(x => x.GymId).NotEmpty();
        RuleFor(x => x.MemberId).NotEmpty();
        RuleFor(x => x.CreatedBy).NotEmpty();
        RuleFor(x => x.Content).NotEmpty().MaximumLength(2000);
    }
}
