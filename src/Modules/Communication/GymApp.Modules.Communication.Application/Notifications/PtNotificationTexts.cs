using System.Globalization;
using GymApp.Modules.Communication.Domain.ValueObjects;

namespace GymApp.Modules.Communication.Application.Notifications;

/// <summary>
/// Wording of the PT module's notifications (renewal reminders, trial alerts, session
/// confirmation), Arabic first with the English counterpart. See <see cref="NotificationTexts"/>.
/// </summary>
internal static class PtNotificationTexts
{
    private static readonly CultureInfo Inv = CultureInfo.InvariantCulture;

    public static NotificationText RenewalReminderForMember(int remaining) => remaining == 1
        ? new(
            "تبقت حصة واحدة",
            "تبقت لك حصة واحدة في باقتك. جدّد الآن حتى لا تتوقف عن التمرين.",
            "1 session left",
            "You have 1 session left in your package. Renew now to keep training.")
        : new(
            $"تبقت {remaining.ToString(Inv)} حصص",
            $"تبقت لك {remaining.ToString(Inv)} حصص في باقتك. فكّر في التجديد قريبًا.",
            $"{remaining.ToString(Inv)} sessions left",
            $"You have {remaining.ToString(Inv)} sessions left in your package. Consider renewing soon.");

    public static NotificationText RenewalReminderForStaff(string memberName, int remaining) => new(
        "تذكير بالتجديد",
        $"تبقى لدى {memberName} {remaining.ToString(Inv)} حصة في الباقة.",
        "Renewal reminder",
        $"{memberName} has {remaining.ToString(Inv)} session(s) left in their package.");

    public static NotificationText TrialLastSession(string memberName) => new(
        "آخر حصة تجريبية",
        $"الحصة القادمة هي آخر حصة مجانية لـ {memberName}. حان وقت عرض الباقات.",
        "Last free session",
        $"{memberName}'s next session is their last free one. Time to offer a package.");

    public static NotificationText TrialFinished(string memberName) => new(
        "انتهت الحصص التجريبية",
        $"أنهى {memberName} الحصص التجريبية ولم يشترك بعد.",
        "Trial finished",
        $"{memberName} finished the free trial and has not bought a package yet.");

    public static NotificationText SessionConfirmRequest() => new(
        "أكّد حصتك",
        "سجّل مدربك حصة تدريبية لك. اضغط للتأكيد.",
        "Confirm your session",
        "Your coach logged a training session. Tap to confirm.");

    public static NotificationText SessionDisputed(string memberName) => new(
        "اعتراض على حصة",
        $"أفاد {memberName} بأن حصة مسجلة لم تتم. راجعها من قائمة المراجعة.",
        "Session disputed",
        $"{memberName} says a logged session did not happen. Review it in the review list.");
}
