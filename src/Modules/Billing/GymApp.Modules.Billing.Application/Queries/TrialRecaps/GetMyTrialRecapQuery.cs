using GymApp.Common.Application.Messaging;

namespace GymApp.Modules.Billing.Application.Queries.TrialRecaps;

/// <summary>pt.trial_recap: the trainee's latest trial plan from their coach.</summary>
public sealed record GetMyTrialRecapQuery(Guid GymId, Guid MemberId) : IQuery<TrialRecapDto>;

public sealed record TrialRecapDto(
    Guid PackageId,
    Guid TrainerId,
    string TrainerName,
    string? TrainerPhotoUrl,
    string Note,
    int RecommendedSessions,
    DateTime SentAt,
    bool TrialFinished);
