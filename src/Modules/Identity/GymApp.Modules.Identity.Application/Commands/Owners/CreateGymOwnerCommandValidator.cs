using FluentValidation;

namespace GymApp.Modules.Identity.Application.Commands.Owners;

internal sealed class CreateGymOwnerCommandValidator : AbstractValidator<CreateGymOwnerCommand>
{
    public CreateGymOwnerCommandValidator()
    {
        RuleFor(x => x.Email).NotEmpty().EmailAddress();
        RuleFor(x => x.Password).NotEmpty().MinimumLength(8);
        RuleFor(x => x.FirstName).NotEmpty().MaximumLength(100);
        RuleFor(x => x.LastName).NotEmpty().MaximumLength(100);
        RuleFor(x => x.CompanyName).NotEmpty().MaximumLength(200);
        RuleFor(x => x.BillingEmail).NotEmpty().EmailAddress();
        // Tier names mirror billing.subscription_plans codes (case-insensitive
        // here; provisioner lowercases). "Enterprise" kept as a legacy alias
        // and mapped to Business by GymProvisioningService.HydrateBillingAsync.
        RuleFor(x => x.PlanTier).NotEmpty()
            .Must(p => p is "Free" or "Starter" or "Pro" or "Business" or "Enterprise")
            .WithMessage("PlanTier must be one of: Free, Starter, Pro, Business");
        RuleFor(x => x.DashboardMode).NotEmpty()
            .Must(m => m is "Unified" or "PerGym" or "Both")
            .WithMessage("DashboardMode must be one of: Unified, PerGym, Both");
    }
}
