using FluentValidation;

namespace GymApp.Modules.Communication.Application.Commands.UpdateNotificationPreferences;

internal sealed class UpdateNotificationPreferencesCommandValidator : AbstractValidator<UpdateNotificationPreferencesCommand>
{
    public UpdateNotificationPreferencesCommandValidator()
    {
        RuleFor(x => x.UserId).NotEmpty();
        RuleFor(x => x.Preferences).NotNull();
        RuleForEach(x => x.Preferences).ChildRules(pref =>
        {
            pref.RuleFor(p => p.NotificationType).NotEmpty();
        });
    }
}
