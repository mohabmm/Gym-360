using GymApp.Common.Application.Messaging;

namespace GymApp.Modules.Billing.Application.Commands.MembershipPlans;

public sealed record UpdateMembershipPlanCommand(
    Guid PlanId,
    string? Name,
    int? DurationDays,
    decimal? Price,
    bool? IsActive,
    List<string>? Features) : ICommand;
