using GymApp.Common.Application.Messaging;

namespace GymApp.Modules.Billing.Application.Commands.PlanFeatures;

public sealed record TogglePlanFeatureCommand(
    Guid PlanId,
    Guid FeatureId,
    bool IsEnabled,
    int? LimitValue) : ICommand;
