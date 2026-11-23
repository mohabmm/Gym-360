using GymApp.Modules.Communication.Domain.ValueObjects;

namespace GymApp.Modules.Communication.Application.Notifications;

/// <summary>pt.training_plan notification wording, Arabic first.</summary>
internal static class TrainingPlanTexts
{
    public static NotificationText Assigned(string coachName, string planName) => new(
        "خطة تدريبك الجديدة جاهزة",
        $"جهّز لك الكابتن {coachName} خطة \"{planName}\". افتح التطبيق وشوف تمرين النهارده.",
        "Your new training plan is ready",
        $"Coach {coachName} made you the plan \"{planName}\". Open the app to see today's workout.");

    public static NotificationText Updated(string coachName, string planName) => new(
        "تم تعديل خطتك",
        $"عدّل الكابتن {coachName} خطة \"{planName}\". التمرين الجاي بالتعديلات الجديدة.",
        "Your plan was updated",
        $"Coach {coachName} changed \"{planName}\". Your next workout has the changes.");
}
