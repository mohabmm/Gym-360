using GymApp.Common.Application.Messaging;

namespace GymApp.Modules.Billing.Application.Commands.GymSubscriptions;

public sealed record ChangeGymPlanCommand(
    Guid GymId,
    Guid PlanId,
    string Reason,
    Guid ChangedBy) : ICommand;
