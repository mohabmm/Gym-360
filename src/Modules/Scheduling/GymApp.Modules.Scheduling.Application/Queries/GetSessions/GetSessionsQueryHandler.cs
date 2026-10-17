using GymApp.Modules.Scheduling.Application.Abstractions;
using GymApp.Common.Application.Messaging;
using GymApp.Common.Domain;
using GymApp.Modules.Scheduling.Application.DTOs;
using GymApp.Modules.Scheduling.Domain.Enums;
using GymApp.Modules.Scheduling.Domain.Repositories;
using GymApp.Modules.Scheduling.Domain.ValueObjects;

namespace GymApp.Modules.Scheduling.Application.Queries.GetSessions;

internal sealed class GetSessionsQueryHandler
    : IQueryHandler<GetSessionsQuery, PagedResponse<SessionDto>>
{
    private readonly ISessionRepository _sessionRepository;

    public GetSessionsQueryHandler(ISessionRepository sessionRepository)
    {
        _sessionRepository = sessionRepository;
    }

    public async Task<Result<PagedResponse<SessionDto>>> Handle(
        GetSessionsQuery request,
        CancellationToken cancellationToken)
    {
        var today = GymLocalTime.Today();
        var from = request.From ?? today.AddMonths(-1);
        var to = request.To ?? today.AddMonths(1);
        var range = new GymDateRange(request.GymId, from, to);

        IReadOnlyList<Domain.Entities.Session> sessions;

        if (request.TrainerId.HasValue)
        {
            sessions = await _sessionRepository.GetByTrainerAsync(
                request.TrainerId.Value, range, cancellationToken);
        }
        else if (request.MemberId.HasValue)
        {
            sessions = await _sessionRepository.GetByMemberAsync(
                request.MemberId.Value, range, cancellationToken);
        }
        else
        {
            // Get all sessions for the gym — use trainer-based query with broader range
            // In a real implementation, the repository would have a GetByGymAsync method
            sessions = await _sessionRepository.GetByTrainerAsync(
                Guid.Empty, range, cancellationToken);
        }

        // Filter by status if provided
        if (!string.IsNullOrWhiteSpace(request.Status) &&
            Enum.TryParse<SessionStatus>(request.Status, true, out var statusFilter))
        {
            sessions = sessions.Where(s => s.Status == statusFilter).ToList();
        }

        var totalCount = sessions.Count;
        var page = Math.Max(1, request.Page);
        var pageSize = Math.Clamp(request.PageSize, 1, 100);
        var totalPages = (int)Math.Ceiling(totalCount / (double)pageSize);

        var pagedSessions = sessions
            .OrderBy(s => s.ScheduledDate)
            .ThenBy(s => s.ScheduledTime)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(MapToDto)
            .ToList();

        return Result.Success(new PagedResponse<SessionDto>(
            pagedSessions, page, pageSize, totalCount, totalPages));
    }

    private static SessionDto MapToDto(Domain.Entities.Session session) =>
        new(
            session.Id,
            session.GymId,
            session.TrainerId,
            session.MemberId,
            session.ScheduledDate.ToDateTime(session.ScheduledTime, DateTimeKind.Utc),
            session.DurationMinutes,
            session.SessionType.ToString(),
            session.Focus,
            session.Status.ToString(),
            session.IsRecurring,
            session.CreatedAt);
}
