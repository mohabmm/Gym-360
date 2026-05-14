using FluentValidation;

namespace GymApp.Modules.Billing.Application.Commands.SessionPackages;

internal sealed class QuickAssignSessionPackageCommandValidator : AbstractValidator<QuickAssignSessionPackageCommand>
{
    public QuickAssignSessionPackageCommandValidator()
    {
        RuleFor(x => x.GymId).NotEmpty();
        RuleFor(x => x.TrainerId).NotEmpty();
        RuleFor(x => x.MemberId).NotEmpty();
        RuleFor(x => x.AssignedBy).NotEmpty();
        // Multiples of 4 only, range 4..40 (matches the staff-app dropdown).
        // Higher / lower values must come from a real PackageTemplate via the
        // existing /billing/packages/assign route.
        RuleFor(x => x.SessionCount)
            .Must(c => c >= 4 && c <= 40 && c % 4 == 0)
            .WithMessage("SessionCount must be a multiple of 4 between 4 and 40.");
        RuleFor(x => x.TotalPrice!.Value)
            .GreaterThanOrEqualTo(0)
            .When(x => x.TotalPrice.HasValue)
            .WithMessage("TotalPrice must be non-negative.");
    }
}
