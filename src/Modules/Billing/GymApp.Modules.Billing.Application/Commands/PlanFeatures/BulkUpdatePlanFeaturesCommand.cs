using GymApp.Common.Application.Messaging;
using GymApp.Modules.Billing.Application.DTOs;

namespace GymApp.Modules.Billing.Application.Commands.PlanFeatures;

public sealed record BulkUpdatePlanFeaturesCommand(
    Guid PlanId,
    List<PlanFeatureToggleDto> Features) : ICommand;
