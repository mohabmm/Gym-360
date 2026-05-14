using FluentValidation;

namespace GymApp.Modules.Gyms.Application.Commands.UpdateGym;

internal sealed class UpdateGymCommandValidator : AbstractValidator<UpdateGymCommand>
{
    public UpdateGymCommandValidator()
    {
        RuleFor(x => x.GymId).NotEmpty();
        RuleFor(x => x.Name).MaximumLength(200).When(x => x.Name is not null);
        RuleFor(x => x.Description).MaximumLength(1000).When(x => x.Description is not null);
        RuleFor(x => x.Address!.Street).NotEmpty().MaximumLength(200).When(x => x.Address is not null);
        RuleFor(x => x.Address!.City).NotEmpty().MaximumLength(100).When(x => x.Address is not null);
        RuleFor(x => x.Address!.Country).NotEmpty().MaximumLength(100).When(x => x.Address is not null);
        RuleFor(x => x.Contact!.Email).NotEmpty().EmailAddress().When(x => x.Contact is not null);
        RuleFor(x => x.Contact!.Phone).NotEmpty().MaximumLength(20).When(x => x.Contact is not null);
    }
}
