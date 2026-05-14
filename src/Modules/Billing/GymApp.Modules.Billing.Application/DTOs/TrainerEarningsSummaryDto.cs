namespace GymApp.Modules.Billing.Application.DTOs;

public sealed record TrainerEarningsSummaryDto(
    Guid GymId,
    Guid TrainerId,
    int TotalSessions,
    decimal TotalEarningsEgp,
    string Currency,
    DateTime PeriodFrom,
    DateTime PeriodTo,
    IReadOnlyList<EarningsRecordDto> Records);

public sealed record EarningsRecordDto(
    Guid SessionId,
    Guid MemberId,
    decimal RateApplied,
    string Currency,
    DateTime EarnedAt);
