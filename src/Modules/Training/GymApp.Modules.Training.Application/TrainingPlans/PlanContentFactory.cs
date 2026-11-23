using GymApp.Common.Domain;
using GymApp.Modules.Training.Domain.Enums;
using GymApp.Modules.Training.Domain.Errors;
using GymApp.Modules.Training.Domain.ValueObjects;

namespace GymApp.Modules.Training.Application.TrainingPlans;

internal static class PlanContentFactory
{
    public static Result<PlanContent> From(PlanContentInput input)
    {
        if (!Enum.TryParse<PlanGoal>(input.Goal, ignoreCase: true, out var goal) || !Enum.IsDefined(goal))
            return Result.Failure<PlanContent>(TrainingPlanErrors.InvalidDocument("Unknown goal."));
        if (input.Document?.Weeks is null)
            return Result.Failure<PlanContent>(TrainingPlanErrors.InvalidDocument("The plan has no weeks."));
        var document = input.RepeatWeekOneForWeeks is { } weeks
            ? input.Document.CopyFirstWeekToAll(Math.Clamp(weeks, 1, PlanDocumentRules.MaxWeeks))
            : input.Document;
        return PlanContent.Create(new PlanContent
        {
            Name = input.Name ?? string.Empty,
            NameAr = input.NameAr,
            Goal = goal,
            GoalText = input.GoalText,
            Document = document
        });
    }

    /// <summary>Same content under another name (save-as-template, duplicate).</summary>
    public static PlanContent Renamed(PlanContent content, string? name, string? nameAr) =>
        string.IsNullOrWhiteSpace(name)
            ? content
            : content with { Name = name.Trim(), NameAr = string.IsNullOrWhiteSpace(nameAr) ? null : nameAr.Trim() };
}
