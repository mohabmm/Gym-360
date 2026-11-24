using GymApp.Common.Application.Messaging;
using GymApp.Common.Domain;
using GymApp.Modules.Billing.Shared;
using GymApp.Modules.Identity.Shared;
using GymApp.Modules.Training.Domain.Repositories;

namespace GymApp.Modules.Training.Application.TrainingPlans.Starter;

/// <summary>The desk's view of one member: each coach with a live package, and whether the programs are open.</summary>
internal sealed class GetMemberProgramUnlocksQueryHandler : IQueryHandler<GetMemberProgramUnlocksQuery, MemberProgramUnlocksDto>
{
    private readonly PlanAccess _access;
    private readonly ITrainerAssignmentLookup _assignments;
    private readonly IPlanProgramUnlockRepository _unlocks;
    private readonly ITrainingPlanRepository _plans;
    private readonly IUserDisplayLookup _users;

    public GetMemberProgramUnlocksQueryHandler(
        PlanAccess access,
        ITrainerAssignmentLookup assignments,
        IPlanProgramUnlockRepository unlocks,
        ITrainingPlanRepository plans,
        IUserDisplayLookup users)
    {
        _access = access;
        _assignments = assignments;
        _unlocks = unlocks;
        _plans = plans;
        _users = users;
    }

    public async Task<Result<MemberProgramUnlocksDto>> Handle(GetMemberProgramUnlocksQuery request, CancellationToken cancellationToken)
    {
        var enabled = await _access.EnsureStarterPlanEnabledAsync(request.GymId, cancellationToken);
        if (enabled.IsFailure)
            return Result.Failure<MemberProgramUnlocksDto>(enabled.Error);

        var coaches = (await _assignments.GetAssignedTrainersAsync(request.TraineeId, new[] { request.GymId }, cancellationToken))
            .Values.Distinct().ToList();
        var unlocks = (await _unlocks.ListForTraineeAsync(request.GymId, request.TraineeId, cancellationToken))
            .ToDictionary(u => u.TrainerId);
        var names = await _users.GetDisplayNamesAsync(coaches, cancellationToken);
        var active = await _plans.GetActiveForTraineeAsync(request.GymId, request.TraineeId, cancellationToken);

        var rows = coaches
            .Select(id => StarterStatus.Row(id, NameOf(names, id), unlocks.GetValueOrDefault(id), active))
            .ToList();
        return Result.Success(new MemberProgramUnlocksDto(request.TraineeId, rows));
    }

    private static string NameOf(IReadOnlyDictionary<Guid, UserDisplay> names, Guid id) =>
        names.TryGetValue(id, out var display) && !string.IsNullOrWhiteSpace(display.FullName) ? display.FullName : "—";
}
