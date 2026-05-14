using FluentValidation;

namespace GymApp.Modules.Billing.Application.Commands.Payments;

internal sealed class RecordPaymentCommandValidator : AbstractValidator<RecordPaymentCommand>
{
    public RecordPaymentCommandValidator()
    {
        RuleFor(x => x.GymId).NotEmpty();
        RuleFor(x => x.MemberId).NotEmpty();
        RuleFor(x => x.Amount).GreaterThan(0);
        RuleFor(x => x.PaymentMethod).NotEmpty();
        RuleFor(x => x.Purpose).NotEmpty();
        RuleFor(x => x.RecordedBy).NotEmpty();

        // Inline session-package creation: only valid when Purpose says
        // SessionPackage and the caller is not pointing at a pre-existing package.
        When(x => string.Equals(x.Purpose, "SessionPackage", System.StringComparison.OrdinalIgnoreCase)
                  && !x.ReferenceId.HasValue, () =>
        {
            RuleFor(x => x.TrainerId)
                .NotNull().WithMessage("TrainerId is required when recording a SessionPackage payment without ReferenceId.");
            RuleFor(x => x.SessionCount)
                .NotNull().WithMessage("SessionCount is required when recording a SessionPackage payment without ReferenceId.")
                .Must(c => !c.HasValue || (c.Value >= 4 && c.Value <= 40 && c.Value % 4 == 0))
                .WithMessage("SessionCount must be a multiple of 4 between 4 and 40.");
        });
    }
}
