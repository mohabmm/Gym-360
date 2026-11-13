using GymApp.Common.Application.Messaging;

namespace GymApp.Modules.Billing.Application.Queries.PtReports;

/// <summary>pt.owner_dashboard — trial-to-paid conversion per coach, renewals and sessions logged in [From, To).</summary>
public sealed record GetPtDashboardQuery(Guid GymId, DateTime FromUtc, DateTime ToUtc) : IQuery<PtDashboardDto>;

public sealed record PtDashboardDto(
    DateTime From,
    DateTime To,
    PtDashboardTotals Totals,
    IReadOnlyList<PtCoachRowDto> Coaches);

public sealed record PtDashboardTotals(
    int TrialsFinished,
    int TrialsConverted,
    decimal ConversionRate,
    int Renewals,
    int NewPaidPackages,
    int SessionsLogged,
    int ActiveTrials,
    int NotConvertedTrials,
    // pt.package_interest: leads waiting for the desk (0 when the flag is off).
    int PendingInterests = 0);

public sealed record PtCoachRowDto(
    Guid TrainerId,
    string TrainerName,
    int TrialsFinished,
    int TrialsConverted,
    decimal ConversionRate,
    int Renewals,
    int NewPaidPackages,
    int SessionsLogged);
