using GymApp.Common.Application.Messaging;

namespace GymApp.Modules.Billing.Application.Commands.Plans;

public sealed record CreatePlanCommand(
    string Code,
    string Name,
    string? Description,
    decimal MonthlyPrice,
    decimal AnnualPrice,
    int MemberLimit,
    int TrainerLimit,
    int StaffLimit,
    bool IsPublic,
    int SortOrder) : ICommand<Guid>;
