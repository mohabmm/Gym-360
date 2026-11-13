using GymApp.Common.Application.Messaging;
using GymApp.Modules.Progress.Application.DTOs;

namespace GymApp.Modules.Progress.Application.Queries.GetProgressJourney;

/// <summary>The member's whole measurement history. TrainerId set = a coach reading a client.</summary>
public sealed record GetProgressJourneyQuery(
    Guid MemberId,
    Guid GymId,
    Guid? TrainerId) : IQuery<ProgressJourneyResponse>;
