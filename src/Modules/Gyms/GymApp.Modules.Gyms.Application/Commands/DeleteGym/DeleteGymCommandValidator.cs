using FluentValidation;

namespace GymApp.Modules.Gyms.Application.Commands.DeleteGym;

internal sealed class DeleteGymCommandValidator : AbstractValidator<DeleteGymCommand>
{
    public DeleteGymCommandValidator()
    {
        RuleFor(x => x.GymId).NotEmpty();
    }
}
