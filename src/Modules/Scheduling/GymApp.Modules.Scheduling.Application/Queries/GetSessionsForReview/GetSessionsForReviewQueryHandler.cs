using GymApp.Common.Application.Messaging;
using GymApp.Common.Domain;
using GymApp.Modules.Scheduling.Application.DTOs;
using GymApp.Modules.Scheduling.Domain.Enums;
using GymApp.Modules.Scheduling.Domain.Repositories;

namespace GymApp.Modules.Scheduling.Application.Queries.GetSessionsForReview;

internal sealed class GetSessionsForReviewQueryHandler
    : IQueryHandler<GetSessionsForReviewQuery, IReadOnlyList<SessionDto>>
{
    private readonly ISessionRepository _sessions;

    public GetSessionsForReviewQueryHandler(ISessionRepository sessions)
    {
        _sessions = sessions;
    }

    public async Task<Result<IReadOnlyList<SessionDto>>> Handle(
        GetSessionsForReviewQuery request, CancellationToken cancellationToken)
    {
        var status = Enum.TryParse<MemberConfirmationStatus>(request.MemberConfirmation, true, out var parsed)
            ? parsed
            : MemberConfirmationStatus.Disputed;

        var sessions = await _sessions.GetByMemberConfirmationAsync(request.GymId, status, cancellationToken);

        IReadOnlyList<SessionDto> dtos = sessions
            .Select(s => new SessionDto(
                s.Id,
                s.GymId,
                s.TrainerId,
                s.MemberId,
                s.ScheduledDate.ToDateTime(s.ScheduledTime, DateTimeKind.Utc),
                s.DurationMinutes,
                s.SessionType.ToString(),
                s.Focus,
                s.Status.ToString(),
                s.IsRecurring,
                s.CreatedAt,
                s.MemberConfirmation.ToString(),
                s.IsQuickLog))
            .ToList();

        return Result.Success(dtos);
    }
}
