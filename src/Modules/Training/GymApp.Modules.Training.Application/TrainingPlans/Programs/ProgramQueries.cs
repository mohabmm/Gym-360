using GymApp.Common.Application.Messaging;
using GymApp.Common.Domain;
using GymApp.Modules.Training.Application.TrainingPlans.Templates;
using GymApp.Modules.Training.Domain.Errors;
using GymApp.Modules.Training.Domain.Repositories;
using GymApp.Modules.Training.Domain.ValueObjects;

namespace GymApp.Modules.Training.Application.TrainingPlans.Programs;

/// <summary>A ready program in the picker. <paramref name="Category"/>: Classic, Egyptian, Lifestyle or Cardio.</summary>
public sealed record PlanProgramSummaryDto(
    string Key,
    string Name,
    string NameAr,
    string Description,
    string DescriptionAr,
    string Category,
    string Level,
    string Goal,
    int DurationWeeks,
    int DaysPerWeek);

/// <summary>The program's days, linked to the gym's exercise library — ready for the builder.</summary>
public sealed record PlanProgramDto(PlanProgramSummaryDto Summary, PlanDocument Document);

public sealed record ListPlanProgramsQuery(PlanActor Actor) : IQuery<IReadOnlyList<PlanProgramSummaryDto>>;

public sealed record GetPlanProgramQuery(PlanActor Actor, string Key) : IQuery<PlanProgramDto>;

internal sealed class PlanProgramQueryHandler
    : IQueryHandler<ListPlanProgramsQuery, IReadOnlyList<PlanProgramSummaryDto>>,
      IQueryHandler<GetPlanProgramQuery, PlanProgramDto>
{
    private readonly PlanAccess _access;
    private readonly IExerciseRepository _exercises;

    public PlanProgramQueryHandler(PlanAccess access, IExerciseRepository exercises)
    {
        _access = access;
        _exercises = exercises;
    }

    public async Task<Result<IReadOnlyList<PlanProgramSummaryDto>>> Handle(ListPlanProgramsQuery request, CancellationToken cancellationToken)
    {
        var enabled = await _access.EnsureEnabledAsync(request.Actor.GymId, cancellationToken);
        return enabled.IsFailure
            ? Result.Failure<IReadOnlyList<PlanProgramSummaryDto>>(enabled.Error)
            : Result.Success<IReadOnlyList<PlanProgramSummaryDto>>(ProgramCatalog.All.Select(ToSummary).ToList());
    }

    public async Task<Result<PlanProgramDto>> Handle(GetPlanProgramQuery request, CancellationToken cancellationToken)
    {
        var enabled = await _access.EnsureEnabledAsync(request.Actor.GymId, cancellationToken);
        if (enabled.IsFailure)
            return Result.Failure<PlanProgramDto>(enabled.Error);
        var program = ProgramCatalog.Find(request.Key);
        if (program is null)
            return Result.Failure<PlanProgramDto>(TrainingPlanErrors.TemplateNotFound);
        var library = await GlobalExerciseLookup.LoadAsync(_exercises, request.Actor.GymId, cancellationToken);
        return Result.Success(new PlanProgramDto(ToSummary(program), program.ToContent(library).Document));
    }

    internal static PlanProgramSummaryDto ToSummary(ReadyProgram p) => new(
        p.Key, p.Text.NameEn, p.Text.NameAr, p.Text.DescriptionEn, p.Text.DescriptionAr,
        p.Profile.Category.ToString(), p.Profile.Level.ToString(), p.Profile.Goal.ToString(),
        p.Profile.Weeks, p.DaysPerWeek);
}
