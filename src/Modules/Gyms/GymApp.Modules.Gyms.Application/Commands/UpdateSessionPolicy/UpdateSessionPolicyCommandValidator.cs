using FluentValidation;

namespace GymApp.Modules.Gyms.Application.Commands.UpdateSessionPolicy;

internal sealed class UpdateSessionPolicyCommandValidator : AbstractValidator<UpdateSessionPolicyCommand>
{
    public UpdateSessionPolicyCommandValidator()
    {
        RuleFor(x => x.GymId).NotEmpty();
        RuleFor(x => x.CancellationWindowHours).GreaterThanOrEqualTo(0).LessThanOrEqualTo(168);
        RuleFor(x => x.NoshowThreshold).GreaterThan(0).LessThanOrEqualTo(100);
        RuleFor(x => x.NoshowWindowDays).GreaterThan(0).LessThanOrEqualTo(365);
        RuleFor(x => x.NoshowAction).NotEmpty();
    }
}
