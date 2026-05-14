using FluentValidation;

namespace GymApp.Modules.Gyms.Application.Commands.ReactivateGym;

internal sealed class ReactivateGymCommandValidator : AbstractValidator<ReactivateGymCommand>
{
    public ReactivateGymCommandValidator()
    {
        RuleFor(x => x.GymId).NotEmpty();
    }
}
