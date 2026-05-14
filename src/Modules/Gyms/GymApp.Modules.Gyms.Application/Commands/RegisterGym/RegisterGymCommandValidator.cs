using FluentValidation;

namespace GymApp.Modules.Gyms.Application.Commands.RegisterGym;

internal sealed class RegisterGymCommandValidator : AbstractValidator<RegisterGymCommand>
{
    public RegisterGymCommandValidator()
    {
        RuleFor(x => x.OwnerId).NotEmpty();
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Description).MaximumLength(1000);
        RuleFor(x => x.Address).NotNull();
        RuleFor(x => x.Address.Street).NotEmpty().MaximumLength(200).When(x => x.Address is not null);
        RuleFor(x => x.Address.City).NotEmpty().MaximumLength(100).When(x => x.Address is not null);
        RuleFor(x => x.Address.State).NotEmpty().MaximumLength(100).When(x => x.Address is not null);
        RuleFor(x => x.Address.Country).NotEmpty().MaximumLength(100).When(x => x.Address is not null);
        RuleFor(x => x.Address.ZipCode).NotEmpty().MaximumLength(20).When(x => x.Address is not null);
        RuleFor(x => x.Contact).NotNull();
        RuleFor(x => x.Contact.Email).NotEmpty().EmailAddress().When(x => x.Contact is not null);
        RuleFor(x => x.Contact.Phone).NotEmpty().MaximumLength(20).When(x => x.Contact is not null);
    }
}
