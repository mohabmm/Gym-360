using GymApp.Common.Application.Messaging;

namespace GymApp.Modules.Billing.Application.Commands.Plans;

public sealed record UpdatePlanCommand(
    Guid PlanId,
    string? Name,
    string? Description,
    decimal? MonthlyPrice,
    decimal? AnnualPrice,
    int? MemberLimit,
    int? TrainerLimit,
    int? StaffLimit,
    bool? IsPublic,
    bool? IsActive,
    int? SortOrder) : ICommand;
