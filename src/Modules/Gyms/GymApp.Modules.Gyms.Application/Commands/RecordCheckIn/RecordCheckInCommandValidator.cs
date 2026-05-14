using FluentValidation;

namespace GymApp.Modules.Gyms.Application.Commands.RecordCheckIn;

internal sealed class RecordCheckInCommandValidator : AbstractValidator<RecordCheckInCommand>
{
    public RecordCheckInCommandValidator()
    {
        RuleFor(x => x.GymId).NotEmpty();
        RuleFor(x => x.Method).NotEmpty();
        RuleFor(x => x)
            .Must(x => x.MemberId.HasValue ||
                       !string.IsNullOrWhiteSpace(x.QrToken) ||
                       !string.IsNullOrWhiteSpace(x.MemberNumber))
            .WithMessage("At least one of MemberId, QrToken, or MemberNumber must be provided.");
    }
}
