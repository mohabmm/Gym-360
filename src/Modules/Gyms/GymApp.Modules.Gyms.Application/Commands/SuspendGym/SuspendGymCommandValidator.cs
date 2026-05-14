using FluentValidation;

namespace GymApp.Modules.Gyms.Application.Commands.SuspendGym;

internal sealed class SuspendGymCommandValidator : AbstractValidator<SuspendGymCommand>
{
    public SuspendGymCommandValidator()
    {
        RuleFor(x => x.GymId).NotEmpty();
        RuleFor(x => x.Reason).NotEmpty().MaximumLength(1000);
    }
}
