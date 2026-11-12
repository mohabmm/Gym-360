using GymApp.Common.Application.Messaging;

namespace GymApp.Modules.Billing.Application.Queries.PtReports;

/// <summary>pt.trial_end_alerts — desk list of trainees who finished their free trial and have not bought a package.</summary>
public sealed record GetNotConvertedTrialsQuery(Guid GymId) : IQuery<IReadOnlyList<NotConvertedTrialDto>>;

public sealed record NotConvertedTrialDto(
    Guid PackageId,
    Guid MemberId,
    string MemberName,
    Guid TrainerId,
    string TrainerName,
    DateTime TrialStartedAt,
    DateTime FinishedAt,
    int DaysSinceFinished,
    bool ConversionWindowClosed);
