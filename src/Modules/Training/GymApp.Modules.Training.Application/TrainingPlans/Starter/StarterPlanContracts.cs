using GymApp.Common.Application.Messaging;
using GymApp.Modules.Training.Domain.Entities;
using GymApp.Modules.Training.Domain.Enums;

namespace GymApp.Modules.Training.Application.TrainingPlans.Starter;

/// <summary>pt.starter_plan, for the coach: what may they give this client now (3-day plan, one week, 30 days, or anything)?</summary>
public sealed record GetCoachPlanAccessQuery(PlanActor Actor, Guid TraineeId) : IQuery<CoachPlanAccessDto>;

/// <summary>pt.starter_plan, for the desk: the member's coach(es) and whether a one-week or 30-day program is unlocked.</summary>
public sealed record GetMemberProgramUnlocksQuery(Guid GymId, Guid TraineeId) : IQuery<MemberProgramUnlocksDto>;

/// <summary>
/// pt.starter_plan: the staff unlock (<paramref name="Unlocked"/> true) one program of
/// <paramref name="Length"/> (one week or 30 days) from the templates and ready programs for a
/// coach–member pair, or take the unlock back. Unlocking needs the coach to have a package with the
/// member; the coach giving the program uses it up.
/// </summary>
public sealed record SetProgramUnlockCommand(CoachTrainee Pair, Guid StaffUserId, bool Unlocked, UnlockLength Length = UnlockLength.OneWeek)
    : ICommand<ProgramUnlockDto>;

/// <summary>The member's active plan when it is a 3-day starter plan or an unlocked program.</summary>
/// <param name="Limit"><c>Starter</c>, <c>OneWeek</c> or <c>ThirtyDays</c>.</param>
public sealed record LimitedPlanStatusDto(Guid PlanId, string Limit, DateOnly StartDate, DateOnly EndsOn, bool HasEnded);

/// <param name="Limit">
/// <c>Starter</c>: a 3-day plan built in the builder (no template). <c>OneWeek</c> / <c>ThirtyDays</c>:
/// from a template or ready program, as the staff unlocked it. <c>None</c>: pt.starter_plan is off, anything goes.
/// </param>
/// <param name="LimitedPlan">The client's active limited plan, if any.</param>
public sealed record CoachPlanAccessDto(string Limit, LimitedPlanStatusDto? LimitedPlan)
{
    /// <summary>Calendar days the plan lasts; 0 = no limit.</summary>
    public int LengthDays { get; init; }
    /// <summary>Most plan weeks; 0 = no limit.</summary>
    public int MaxWeeks { get; init; }
    /// <summary>Most workout days a week.</summary>
    public int MaxDaysPerWeek { get; init; }
}

public sealed record ProgramUnlockDto(
    Guid TrainerId,
    string TrainerName,
    bool Unlocked,
    string Length,
    DateTime? ChangedAt,
    LimitedPlanStatusDto? LimitedPlan);

public sealed record MemberProgramUnlocksDto(Guid TraineeId, IReadOnlyList<ProgramUnlockDto> Coaches);
