namespace GymApp.Modules.Billing.Application.DTOs;

public sealed record MemberBalanceResponse(
    Guid GymId,
    Guid MemberId,
    decimal TotalPaid,
    decimal TotalVoided,
    decimal NetBalance,
    string Currency,
    int ActivePackageCount,
    int TotalRemainingSessions);
