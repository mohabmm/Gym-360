using GymApp.Modules.Billing.Application.Abstractions;
using GymApp.Modules.Billing.Domain.Entities;
using GymApp.Modules.Billing.Domain.Repositories;
using GymApp.Modules.Gyms.IntegrationEvents;
using MediatR;
using Microsoft.Extensions.Logging;

namespace GymApp.Modules.Billing.Application.IntegrationEvents;

/// <summary>
/// Auto-subscribes a newly approved gym to the Pro plan if no subscription exists.
/// Without this, plan-gated features (custom_exercise_library, progress_tracking, etc.)
/// would 403 for every freshly registered gym since IsFeatureEnabledAsync returns false
/// when no plan is attached. DemoGymSeeder previously did this only for the demo gym.
/// </summary>
internal sealed class GymApprovedIntegrationEventHandler : INotificationHandler<GymApprovedIntegrationEvent>
{
    private const string DefaultPlanCode = "pro";

    private readonly IGymSubscriptionRepository _subscriptionRepository;
    private readonly ISubscriptionPlanRepository _planRepository;
    private readonly IBillingUnitOfWork _unitOfWork;
    private readonly ILogger<GymApprovedIntegrationEventHandler> _logger;

    public GymApprovedIntegrationEventHandler(
        IGymSubscriptionRepository subscriptionRepository,
        ISubscriptionPlanRepository planRepository,
        IBillingUnitOfWork unitOfWork,
        ILogger<GymApprovedIntegrationEventHandler> logger)
    {
        _subscriptionRepository = subscriptionRepository;
        _planRepository = planRepository;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task Handle(GymApprovedIntegrationEvent notification, CancellationToken cancellationToken)
    {
        var proPlan = await _planRepository.GetByCodeAsync(DefaultPlanCode, cancellationToken);
        if (proPlan is null)
        {
            _logger.LogWarning("Default plan '{PlanCode}' not found; gym {GymId} left without subscription", DefaultPlanCode, notification.GymId);
            return;
        }

        var existing = await _subscriptionRepository.GetByGymIdAsync(notification.GymId, cancellationToken);
        if (existing is not null)
        {
            // Provisioner may have seeded a Free subscription based on the owner's
            // contract tier (or as a fallback). Upgrade to Pro on first approval so
            // plan-gated features work without manual admin intervention. Skip the
            // upgrade if the gym is already on Pro/Business — owners can later
            // downgrade via the admin UI and that choice should stick.
            if (existing.PlanId == proPlan.Id) return;

            var currentPlan = await _planRepository.GetByIdAsync(existing.PlanId, cancellationToken);
            if (currentPlan is not null && !string.Equals(currentPlan.Code, "free", StringComparison.OrdinalIgnoreCase))
                return;

            existing.ChangePlan(proPlan.Id);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
            _logger.LogInformation("Upgraded gym {GymId} from Free to {PlanCode} on approval", notification.GymId, DefaultPlanCode);
            return;
        }

        await _subscriptionRepository.AddAsync(GymSubscription.Create(notification.GymId, proPlan.Id), cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        _logger.LogInformation("Auto-subscribed gym {GymId} to plan {PlanCode}", notification.GymId, DefaultPlanCode);
    }
}
