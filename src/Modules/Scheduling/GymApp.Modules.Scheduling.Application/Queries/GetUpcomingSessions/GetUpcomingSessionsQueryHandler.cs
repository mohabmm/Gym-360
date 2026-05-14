using GymApp.Common.Application.Messaging;
using GymApp.Common.Domain;
using GymApp.Modules.Scheduling.Application.DTOs;
using GymApp.Modules.Scheduling.Domain.Entities;
using GymApp.Modules.Scheduling.Domain.Repositories;

namespace GymApp.Modules.Scheduling.Application.Queries.GetUpcomingSessions;

internal sealed class GetUpcomingSessionsQueryHandler
    : IQueryHandler<GetUpcomingSessionsQuery, List<SessionDto>>
{
    private readonly ISessionRepository _sessionRepository;

    public GetUpcomingSessionsQueryHandler(ISessionRepository sessionRepository)
    {
        _sessionRepository = sessionRepository;
    }

    public async Task<Result<List<SessionDto>>> Handle(
        GetUpcomingSessionsQuery request,
        CancellationToken cancellationToken)
    {
        IReadOnlyList<Session> sessions;

        if (string.Equals(request.Role, "trainer", StringComparison.OrdinalIgnoreCase))
        {
            sessions = await _sessionRepository.GetUpcomingByTrainerAsync(
                request.UserId, cancellationToken);
        }
        else
        {
            sessions = await _sessionRepository.GetUpcomingByMemberAsync(
                request.UserId, cancellationToken);
        }

        var dtos = sessions
            .Where(s => s.GymId == request.GymId)
            .OrderBy(s => s.ScheduledDate)
            .ThenBy(s => s.ScheduledTime)
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
                s.CreatedAt))
            .ToList();

        return Result.Success(dtos);
    }
}
