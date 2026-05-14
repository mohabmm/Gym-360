using FluentValidation;

namespace GymApp.Modules.Gyms.Application.Commands.RejectGym;

internal sealed class RejectGymCommandValidator : AbstractValidator<RejectGymCommand>
{
    public RejectGymCommandValidator()
    {
        RuleFor(x => x.GymId).NotEmpty();
        RuleFor(x => x.Reason).NotEmpty().MaximumLength(1000);
    }
}
