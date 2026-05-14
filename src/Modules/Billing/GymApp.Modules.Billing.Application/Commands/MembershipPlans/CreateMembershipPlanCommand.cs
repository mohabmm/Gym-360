using GymApp.Common.Application.Messaging;

namespace GymApp.Modules.Billing.Application.Commands.MembershipPlans;

public sealed record CreateMembershipPlanCommand(
    Guid GymId,
    string Name,
    int DurationDays,
    decimal Price,
    List<string>? Features) : ICommand<Guid>;
