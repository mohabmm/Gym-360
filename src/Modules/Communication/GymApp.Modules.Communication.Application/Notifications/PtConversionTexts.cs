using System.Globalization;
using GymApp.Modules.Communication.Domain.ValueObjects;

namespace GymApp.Modules.Communication.Application.Notifications;

/// <summary>
/// Wording of the trial-to-paid notifications (pt.trial_recap, pt.package_interest,
/// pt.trial_followups), Arabic first with the English counterpart.
/// </summary>
internal static class PtConversionTexts
{
    private static readonly CultureInfo Inv = CultureInfo.InvariantCulture;

    public static NotificationText RecapForMember(string coachName) => new(
        "خطتك من المدرب جاهزة",
        $"أرسل لك {coachName} خطة تدريبك بعد الحصص التجريبية. اضغط لمشاهدتها.",
        "Your coach's plan is ready",
        $"{coachName} sent you your training plan after the trial. Tap to see it.");

    public static NotificationText InterestForStaff(string memberName, int? sessions) => sessions is { } n
        ? new(
            "عميل مهتم بباقة",
            $"{memberName} مهتم بباقة {n.ToString(Inv)} حصة. تواصل معه لإتمام الاشتراك.",
            "Interested in a package",
            $"{memberName} is interested in a {n.ToString(Inv)}-session package. Contact them to close it.")
        : new(
            "عميل مهتم بباقة",
            $"{memberName} مهتم بالاشتراك في باقة تدريب شخصي. تواصل معه.",
            "Interested in a package",
            $"{memberName} is interested in a PT package. Contact them.");

    public static NotificationText TrialFinishedForMember(string coachName) => new(
        "انتهت حصصك التجريبية",
        $"أحسنت! أكمل رحلتك مع {coachName}. اضغط \"مهتم\" وسيتواصل معك الاستقبال.",
        "Your free sessions are done",
        $"Well done! Keep going with {coachName}. Tap \"I'm interested\" and the desk will contact you.");

    public static NotificationText FollowUpDay3ForMember(string coachName) => new(
        "لا تفقد حماسك",
        $"مرت ٣ أيام على آخر حصة مع {coachName}. استمر الآن قبل أن تفقد ما حققته.",
        "Keep your momentum",
        $"It's been 3 days since your last session with {coachName}. Continue now before you lose your progress.");

    public static NotificationText LastChanceForMember(string coachName) => new(
        "تذكير أخير",
        $"{coachName} مازال يحجز لك مكانًا. اضغط \"مهتم\" ليتواصل معك الاستقبال.",
        "Last reminder",
        $"{coachName} is still keeping a spot for you. Tap \"I'm interested\" and the desk will contact you.");
}
