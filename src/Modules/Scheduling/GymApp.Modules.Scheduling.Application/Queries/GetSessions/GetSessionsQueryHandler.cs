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

        var sessions = Filter(await LoadAsync(request, range, cancellationToken), request);

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

    private Task<IReadOnlyList<Domain.Entities.Session>> LoadAsync(
        GetSessionsQuery request, GymDateRange range, CancellationToken cancellationToken)
    {
        if (request.TrainerId.HasValue)
            return _sessionRepository.GetByTrainerAsync(request.TrainerId.Value, range, cancellationToken);

        if (request.MemberId.HasValue)
            return _sessionRepository.GetByMemberAsync(request.MemberId.Value, range, cancellationToken);

        // Get all sessions for the gym — use trainer-based query with broader range
        // In a real implementation, the repository would have a GetByGymAsync method
        return _sessionRepository.GetByTrainerAsync(Guid.Empty, range, cancellationToken);
    }

    /// <summary>
    /// Optional status filter, and the pt.dual_session_logging confirmation filter
    /// (e.g. ?memberConfirmation=Pending for the trainee's "confirm" list).
    /// </summary>
    private static IReadOnlyList<Domain.Entities.Session> Filter(
        IReadOnlyList<Domain.Entities.Session> sessions, GetSessionsQuery request)
    {
        IEnumerable<Domain.Entities.Session> filtered = sessions;
        if (TryParse<SessionStatus>(request.Status, out var status))
            filtered = filtered.Where(s => s.Status == status);
        if (TryParse<MemberConfirmationStatus>(request.MemberConfirmation, out var confirmation))
            filtered = filtered.Where(s => s.MemberConfirmation == confirmation);
        return filtered.ToList();
    }

    private static bool TryParse<TEnum>(string? value, out TEnum result) where TEnum : struct, Enum
    {
        result = default;
        return !string.IsNullOrWhiteSpace(value) && Enum.TryParse(value, true, out result);
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
            session.CreatedAt,
            session.MemberConfirmation.ToString(),
            session.IsQuickLog);
}
