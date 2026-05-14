using FluentValidation;

namespace GymApp.Modules.Billing.Application.Commands.Deploy;

internal sealed class DeployChangesCommandValidator : AbstractValidator<DeployChangesCommand>
{
    private static readonly string[] ValidScopes = { "gym", "plan", "all" };

    public DeployChangesCommandValidator()
    {
        RuleFor(x => x.Scope)
            .NotEmpty()
            .Must(s => ValidScopes.Contains(s.ToLowerInvariant()))
            .WithMessage("Scope must be one of: gym, plan, all");

        RuleFor(x => x.DeployedBy).NotEmpty();

        RuleFor(x => x.TargetId)
            .NotEmpty()
            .When(x => x.Scope.Equals("gym", StringComparison.OrdinalIgnoreCase))
            .WithMessage("TargetId is required when scope is 'gym'");
    }
}
