using GymApp.Common.Application.Messaging;
using GymApp.Common.Domain;
using GymApp.Modules.Billing.Shared;
using GymApp.Modules.Progress.Application.DTOs;
using GymApp.Modules.Progress.Application.Mappers;
using GymApp.Modules.Progress.Application.Services;
using GymApp.Modules.Progress.Domain.Repositories;
using GymApp.Modules.Progress.Domain.ValueObjects;

namespace GymApp.Modules.Progress.Application.Queries.GetProgressJourney;

internal sealed class GetProgressJourneyQueryHandler : IQueryHandler<GetProgressJourneyQuery, ProgressJourneyResponse>
{
    private readonly IBodyMetricEntryRepository _metrics;
    private readonly IProgressGoalRepository _goals;
    private readonly IGymPlanService _planService;
    private readonly ITrainerAssignmentLookup _assignments;

    public GetProgressJourneyQueryHandler(
        IBodyMetricEntryRepository metrics,
        IProgressGoalRepository goals,
        IGymPlanService planService,
        ITrainerAssignmentLookup assignments)
    {
        _metrics = metrics;
        _goals = goals;
        _planService = planService;
        _assignments = assignments;
    }

    public async Task<Result<ProgressJourneyResponse>> Handle(
        GetProgressJourneyQuery request, CancellationToken cancellationToken)
    {
        var subject = new ProgressSubject(request.MemberId, request.GymId, request.TrainerId);
        var denied = await ProgressAccess.CheckAsync(_planService, _assignments, subject, cancellationToken);
        if (denied != Error.None)
            return Result.Failure<ProgressJourneyResponse>(denied);

        var allTime = new DateRange(DateOnly.MinValue, DateOnly.FromDateTime(DateTime.UtcNow));
        var entries = await _metrics.GetByUserAsync(request.MemberId, request.GymId, allTime, cancellationToken);
        var goal = await _goals.FindAsync(request.MemberId, request.GymId, cancellationToken);

        var ordered = entries
            .OrderBy(e => e.Date)
            .ThenBy(e => e.CreatedAt)
            .Select(ProgressMapper.ToDto)
            .ToList();

        return Result.Success(new ProgressJourneyResponse(
            request.MemberId,
            ordered,
            goal is null ? null : ProgressMapper.ToDto(goal)));
    }
}
