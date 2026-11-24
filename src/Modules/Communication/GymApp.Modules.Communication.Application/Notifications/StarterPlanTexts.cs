using System.Globalization;
using GymApp.Modules.Communication.Domain.ValueObjects;

namespace GymApp.Modules.Communication.Application.Notifications;

/// <summary>pt.starter_plan notification wording for the coach, Arabic first.</summary>
internal static class StarterPlanTexts
{
    private static readonly CultureInfo Inv = CultureInfo.InvariantCulture;

    public static NotificationText NewClient(string memberName, bool isTrial, int sessions) => isTrial
        ? new(
            "عضو جديد معاك 💪",
            $"{memberName} اتسجّل معاك في حصص تجريبية ({sessions.ToString(Inv)}). افتح ملفه وابدأ له خطة ٣ أيام.",
            "New member for you 💪",
            $"{memberName} is booked with you for {sessions.ToString(Inv)} trial sessions. Open their profile and start a 3-day plan.")
        : new(
            "عضو جديد معاك 💪",
            $"{memberName} اشترك معاك في باقة {sessions.ToString(Inv)} حصة. افتح ملفه وابدأ له خطة ٣ أيام.",
            "New member for you 💪",
            $"{memberName} has a {sessions.ToString(Inv)}-session package with you. Open their profile and start a 3-day plan.");

    public static NotificationText ProgramsUnlocked(string memberName, int lengthDays) => lengthDays >= 30
        ? new(
            "اتفتح لك برنامج ٣٠ يوم ✅",
            $"ادّي {memberName} برنامج ٣٠ يوم من القوالب أو البرامج الجاهزة، ومعاه الأكل.",
            "30-day program unlocked ✅",
            $"Give {memberName} a 30-day program from the templates or ready programs, with a diet.")
        : new(
            "اتفتح لك برنامج أسبوع ✅",
            $"{memberName} محتاج يرجع الجيم: ادّيله برنامج أسبوع من القوالب أو البرامج الجاهزة، ومعاه الأكل.",
            "One-week program unlocked ✅",
            $"{memberName} needs to come back: give them one week from the templates or ready programs, with a diet.");
}
