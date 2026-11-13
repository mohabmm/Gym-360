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
            .When(x => !x.IsTrial)
            .WithMessage("SessionCount must be a multiple of 4 between 4 and 40.");
        // Free trial: the desk picks 1–3 sessions; no price and no payment.
        RuleFor(x => x.SessionCount)
            .InclusiveBetween(1, 3)
            .When(x => x.IsTrial)
            .WithMessage("A trial has 1 to 3 free sessions.");
        RuleFor(x => x.TotalPrice)
            .Must(p => p is null or 0)
            .When(x => x.IsTrial)
            .WithMessage("A trial is free.");
        RuleFor(x => x.PaymentMethod)
            .Empty()
            .When(x => x.IsTrial)
            .WithMessage("A trial is free; no payment is recorded.");
        RuleFor(x => x.TotalPrice!.Value)
            .GreaterThanOrEqualTo(0)
            .When(x => x.TotalPrice.HasValue)
            .WithMessage("TotalPrice must be non-negative.");
    }
}
