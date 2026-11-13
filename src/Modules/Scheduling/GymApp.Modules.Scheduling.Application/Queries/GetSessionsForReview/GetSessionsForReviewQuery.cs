using GymApp.Common.Application.Messaging;
using GymApp.Modules.Scheduling.Application.DTOs;

namespace GymApp.Modules.Scheduling.Application.Queries.GetSessionsForReview;

/// <summary>Desk list of sessions by trainee-confirmation state (default: Disputed).</summary>
public sealed record GetSessionsForReviewQuery(Guid GymId, string? MemberConfirmation) : IQuery<IReadOnlyList<SessionDto>>;
