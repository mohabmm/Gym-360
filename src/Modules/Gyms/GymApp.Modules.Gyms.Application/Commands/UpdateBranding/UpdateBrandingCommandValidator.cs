using FluentValidation;

namespace GymApp.Modules.Gyms.Application.Commands.UpdateBranding;

internal sealed class UpdateBrandingCommandValidator : AbstractValidator<UpdateBrandingCommand>
{
    public UpdateBrandingCommandValidator()
    {
        RuleFor(x => x.GymId).NotEmpty();
        RuleFor(x => x.LogoUrl).MaximumLength(500).When(x => x.LogoUrl is not null);
        RuleFor(x => x.PrimaryColor).MaximumLength(20).When(x => x.PrimaryColor is not null);
        RuleFor(x => x.SecondaryColor).MaximumLength(20).When(x => x.SecondaryColor is not null);
        RuleFor(x => x.CoverPhotoUrl).MaximumLength(500).When(x => x.CoverPhotoUrl is not null);
    }
}
