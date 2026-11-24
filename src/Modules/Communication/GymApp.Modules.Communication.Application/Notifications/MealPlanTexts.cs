using GymApp.Modules.Communication.Domain.ValueObjects;

namespace GymApp.Modules.Communication.Application.Notifications;

/// <summary>Diet plan notification wording, Arabic first.</summary>
internal static class MealPlanTexts
{
    public static NotificationText Assigned(string coachName, string planName, int calories) => new(
        "خطة أكلك جاهزة 🍽",
        $"جهّز لك الكابتن {coachName} خطة \"{planName}\" على قد احتياجك: {calories} سعرة في اليوم. افتح التطبيق وشوف أكل النهارده.",
        "Your diet plan is ready 🍽",
        $"Coach {coachName} made you \"{planName}\" for your needs: {calories} kcal a day. Open the app to see today's meals.");
}
