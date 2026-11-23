using GymApp.Common.Domain;
using GymApp.Modules.Training.Domain.Enums;
using GymApp.Modules.Training.Domain.Errors;

namespace GymApp.Modules.Training.Domain.ValueObjects;

/// <summary>What a plan and a template have in common: name, goal and the workout days.</summary>
public sealed record PlanContent
{
    public required string Name { get; init; }
    public string? NameAr { get; init; }
    public required PlanGoal Goal { get; init; }
    /// <summary>Free-text goal, kept only for <see cref="PlanGoal.Other"/>.</summary>
    public string? GoalText { get; init; }
    public required PlanDocument Document { get; init; }

    public int DurationWeeks => Document.DurationWeeks;

    public int DaysPerWeek => Document.Weeks.Count == 0 ? 0 : Document.Weeks.Max(w => w.Days.Count);

    /// <summary>Trims the text, normalizes the document and checks every limit.</summary>
    public static Result<PlanContent> Create(PlanContent draft)
    {
        var name = draft.Name.OrNull();
        if (name is null || name.Length > PlanDocumentRules.MaxNameLength)
            return Fail("The plan needs a name.");
        var goalText = draft.Goal == PlanGoal.Other ? draft.GoalText.OrNull() : null;
        if (draft.Goal == PlanGoal.Other && goalText is null)
            return Fail("Describe the goal.");
        var document = draft.Document.Normalized();
        var valid = PlanDocumentRules.Validate(document);
        return valid.IsFailure
            ? Result.Failure<PlanContent>(valid.Error)
            : Result.Success(draft with { Name = name, NameAr = draft.NameAr.OrNull(), GoalText = goalText, Document = document });
    }

    private static Result<PlanContent> Fail(string reason) =>
        Result.Failure<PlanContent>(TrainingPlanErrors.InvalidDocument(reason));
}
