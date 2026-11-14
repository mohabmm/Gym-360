using FluentValidation;

namespace GymApp.Modules.Billing.Application.Commands.SessionPackages;

internal sealed class QuickAssignSessionPackageCommandValidator : AbstractValidator<QuickAssignSessionPackageCommand>
{
    internal const int MinSessions = 1;
    internal const int MaxSessions = 100;
    internal const int MaxTrialSessions = 20;

    public QuickAssignSessionPackageCommandValidator()
    {
        RuleFor(x => x.GymId).NotEmpty();
        RuleFor(x => x.TrainerId).NotEmpty();
        RuleFor(x => x.MemberId).NotEmpty();
        RuleFor(x => x.AssignedBy).NotEmpty();
        // The desk picks any session count (free or paid) when adding a member
        // or assigning a coach; larger bundles come from a PackageTemplate.
        RuleFor(x => x.SessionCount)
            .InclusiveBetween(MinSessions, MaxSessions)
            .When(x => !x.IsTrial)
            .WithMessage($"SessionCount must be between {MinSessions} and {MaxSessions}.");
        // Free trial: the desk picks 1–20 sessions; no price and no payment.
        RuleFor(x => x.SessionCount)
            .InclusiveBetween(MinSessions, MaxTrialSessions)
            .When(x => x.IsTrial)
            .WithMessage($"A trial has {MinSessions} to {MaxTrialSessions} free sessions.");
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
