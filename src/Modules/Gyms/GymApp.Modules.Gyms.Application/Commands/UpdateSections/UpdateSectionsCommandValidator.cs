using FluentValidation;

namespace GymApp.Modules.Gyms.Application.Commands.UpdateSections;

internal sealed class UpdateSectionsCommandValidator : AbstractValidator<UpdateSectionsCommand>
{
    public UpdateSectionsCommandValidator()
    {
        RuleFor(x => x.GymId).NotEmpty();
        RuleFor(x => x.Sections).NotNull();
    }
}
