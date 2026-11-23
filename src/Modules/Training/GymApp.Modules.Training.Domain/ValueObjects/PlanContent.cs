using GymApp.Common.Domain;
using GymApp.Modules.Training.Domain.Enums;
using GymApp.Modules.Training.Domain.Errors;

namespace GymApp.Modules.Training.Domain.ValueObjects;

/// <summary>What a plan and a template have in common: name, goal and the workout days.</summary>
public sealed record PlanContent(string Name, string? NameAr, PlanGoal Goal, string? GoalText, PlanDocument Document)
{
    /// <summary>Trims the text, normalizes the document and checks every limit.</summary>
    public static Result<PlanContent> Create(string name, string? nameAr, PlanGoal goal, string? goalText, PlanDocument document)
    {
        if (string.IsNullOrWhiteSpace(name) || name.Trim().Length > PlanDocumentRules.MaxNameLength)
            return Result.Failure<PlanContent>(TrainingPlanErrors.InvalidDocument("The plan needs a name."));
        if (goal == PlanGoal.Other && string.IsNullOrWhiteSpace(goalText))
            return Result.Failure<PlanContent>(TrainingPlanErrors.InvalidDocument("Describe the goal."));
        var normalized = document.Normalized();
        var valid = PlanDocumentRules.Validate(normalized);
        if (valid.IsFailure)
            return Result.Failure<PlanContent>(valid.Error);
        return Result.Success(new PlanContent(
            name.Trim(),
            string.IsNullOrWhiteSpace(nameAr) ? null : nameAr.Trim(),
            goal,
            goal == PlanGoal.Other ? goalText!.Trim() : null,
            normalized));
    }

    public int DurationWeeks => Document.DurationWeeks;

    public int DaysPerWeek => Document.Weeks.Count == 0 ? 0 : Document.Weeks.Max(w => w.Days.Count);
}
