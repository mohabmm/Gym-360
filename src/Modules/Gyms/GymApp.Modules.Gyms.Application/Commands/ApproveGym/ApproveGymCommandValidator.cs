using FluentValidation;

namespace GymApp.Modules.Gyms.Application.Commands.ApproveGym;

internal sealed class ApproveGymCommandValidator : AbstractValidator<ApproveGymCommand>
{
    public ApproveGymCommandValidator()
    {
        RuleFor(x => x.GymId).NotEmpty();
    }
}
