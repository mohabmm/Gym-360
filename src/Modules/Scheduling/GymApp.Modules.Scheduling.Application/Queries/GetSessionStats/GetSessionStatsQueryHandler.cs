using GymApp.Modules.Scheduling.Application.Abstractions;
using GymApp.Common.Application.Messaging;
using GymApp.Common.Domain;
using GymApp.Modules.Scheduling.Application.DTOs;
using GymApp.Modules.Scheduling.Domain.Enums;
using GymApp.Modules.Scheduling.Domain.Repositories;

namespace GymApp.Modules.Scheduling.Application.Queries.GetSessionStats;

internal sealed class GetSessionStatsQueryHandler
    : IQueryHandler<GetSessionStatsQuery, SessionStatsResponse>
{
    private readonly ISessionRepository _sessionRepository;

    public GetSessionStatsQueryHandler(ISessionRepository sessionRepository)
    {
        _sessionRepository = sessionRepository;
    }

    public async Task<Result<SessionStatsResponse>> Handle(
        GetSessionStatsQuery request,
        CancellationToken cancellationToken)
    {
        var today = GymLocalTime.Today();
        var from = request.From ?? today.AddMonths(-1);
        var to = request.To ?? today;

        var completedCount = await _sessionRepository.GetCountByStatusAsync(
            request.TrainerId, request.GymId, SessionStatus.Completed, from, to, cancellationToken);

        var cancelledByTrainer = await _sessionRepository.GetCountByStatusAsync(
            request.TrainerId, request.GymId, SessionStatus.CancelledByTrainer, from, to, cancellationToken);

        var cancelledByMember = await _sessionRepository.GetCountByStatusAsync(
            request.TrainerId, request.GymId, SessionStatus.CancelledByMember, from, to, cancellationToken);

        var lateCancelled = await _sessionRepository.GetCountByStatusAsync(
            request.TrainerId, request.GymId, SessionStatus.LateCancelled, from, to, cancellationToken);

        var noShowCount = await _sessionRepository.GetCountByStatusAsync(
            request.TrainerId, request.GymId, SessionStatus.NoShow, from, to, cancellationToken);

        var cancelledCount = cancelledByTrainer + cancelledByMember + lateCancelled;
        var totalScheduled = completedCount + cancelledCount + noShowCount;
        var completionRate = totalScheduled > 0
            ? Math.Round((double)completedCount / totalScheduled * 100, 2)
            : 0;

        return Result.Success(new SessionStatsResponse(
            completedCount,
            cancelledCount,
            noShowCount,
            totalScheduled,
            completionRate));
    }
}
