using GymApp.Common.Application.Messaging;

namespace GymApp.Modules.Billing.Application.Commands.Plans;

public sealed record DeletePlanCommand(Guid PlanId) : ICommand;
