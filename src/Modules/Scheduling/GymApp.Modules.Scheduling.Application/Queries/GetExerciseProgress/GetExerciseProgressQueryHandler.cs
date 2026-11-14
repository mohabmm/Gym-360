using GymApp.Common.Application.FeatureFlags;
using GymApp.Common.Application.Messaging;
using GymApp.Common.Domain;
using GymApp.Modules.Billing.Shared;
using GymApp.Modules.Scheduling.Application.DTOs;
using GymApp.Modules.Scheduling.Domain.Errors;
using GymApp.Modules.Scheduling.Domain.Repositories;
using GymApp.Modules.Scheduling.Domain.ValueObjects;

namespace GymApp.Modules.Scheduling.Application.Queries.GetExerciseProgress;

internal sealed class GetExerciseProgressQueryHandler : IQueryHandler<GetExerciseProgressQuery, ExerciseProgressDto>
{
    private readonly ISessionRepository _sessions;
    private readonly IPtProgressScopeLookup _scopeLookup;
    private readonly ITrainerAssignmentLookup _assignments;
    private readonly IFeatureFlagService _featureFlags;

    public GetExerciseProgressQueryHandler(
        ISessionRepository sessions,
        IPtProgressScopeLookup scopeLookup,
        ITrainerAssignmentLookup assignments,
        IFeatureFlagService featureFlags)
    {
        _sessions = sessions;
        _scopeLookup = scopeLookup;
        _assignments = assignments;
        _featureFlags = featureFlags;
    }

    public async Task<Result<ExerciseProgressDto>> Handle(GetExerciseProgressQuery request, CancellationToken cancellationToken)
    {
        if (!await _featureFlags.IsEnabledAsync(request.GymId, FeatureKeys.PtExerciseProgress, cancellationToken))
            return Result.Failure<ExerciseProgressDto>(FeatureFlagErrors.FeatureDisabled(FeatureKeys.PtExerciseProgress));

        if (!await CanReadAsync(request, cancellationToken))
            return Result.Failure<ExerciseProgressDto>(SchedulingErrors.ExerciseProgressForbidden);

        var sessions = await _sessions.GetCompletedWithExerciseLogsByMemberAsync(
            new MemberInGym(request.GymId, request.MemberId), cancellationToken);
        var scope = await _scopeLookup.GetScopeAsync(request.MemberId, request.GymId, cancellationToken);

        return Result.Success(ExerciseProgressCalculator.Build(sessions, scope));
    }

    // The member, a coach who trained them here, or the coach of their active package.
    private async Task<bool> CanReadAsync(GetExerciseProgressQuery request, CancellationToken cancellationToken)
    {
        if (request.CallerId == request.MemberId)
            return true;

        var participants = new SessionParticipants(request.GymId, request.CallerId, request.MemberId);
        if (await _sessions.HasSessionWithMemberAsync(participants, cancellationToken))
            return true;

        var clients = await _assignments.GetClientsForTrainerAsync(request.CallerId, request.GymId, cancellationToken);
        return clients.Contains(request.MemberId);
    }
}
