using System.Globalization;
using GymApp.Modules.Communication.Domain.ValueObjects;

namespace GymApp.Modules.Communication.Application.Notifications;

/// <summary>
/// Wording of every system-generated notification, Arabic first (the product's primary
/// language) with the English counterpart. Kept in one place so the two stay in step.
/// Numbers and dates are formatted culture-invariantly so they read the same on any host.
/// </summary>
internal static class NotificationTexts
{
    private static readonly CultureInfo Inv = CultureInfo.InvariantCulture;

    public static NotificationText SessionCompletedForTrainer() => new(
        "اكتملت الجلسة",
        "تم تسجيل جلسة تدريبية كمكتملة.",
        "Session Completed",
        "A training session has been marked as completed.");

    public static NotificationText SessionCompletedForMember() => new(
        "اكتملت الجلسة",
        "اكتملت جلستك التدريبية. عمل رائع!",
        "Session Completed",
        "Your training session has been completed. Great work!");

    public static NotificationText SessionBooked(DateTime scheduledAt) => new(
        "حجز جلسة جديدة",
        $"قام أحد الأعضاء بحجز جلسة معك يوم {scheduledAt.ToString("dd/MM/yyyy", Inv)} الساعة {scheduledAt.ToString("HH:mm", Inv)}.",
        "New Session Booked",
        $"A member has booked a session with you on {scheduledAt.ToString("MMM dd, yyyy 'at' HH:mm", Inv)}.");

    public static NotificationText SessionPackageLow(int remaining) => new(
        "باقة الجلسات على وشك الانتهاء",
        $"تبقى لك {remaining.ToString(Inv)} جلسات فقط في باقتك.",
        "Session Package Running Low",
        $"You have only {remaining.ToString(Inv)} sessions remaining in your package.");

    public static NotificationText WorkoutAssigned(string workoutName) => new(
        "تمرين جديد",
        $"قام مدربك بتعيين تمرين جديد لك: {workoutName}",
        "New Workout Assigned",
        $"Your trainer assigned a new workout: {workoutName}");

    public static NotificationText StreakMilestone(int milestoneDay, string streakType) => new(
        "إنجاز جديد!",
        $"مبروك! وصلت إلى {milestoneDay.ToString(Inv)} يوم متتالي.",
        "Streak Milestone!",
        $"Congratulations! You've reached a {milestoneDay.ToString(Inv)}-day {streakType} streak!");

    public static NotificationText MembershipExpiring(DateTime expiresAt) => new(
        "اشتراكك على وشك الانتهاء",
        $"ينتهي اشتراكك يوم {expiresAt.ToString("dd/MM/yyyy", Inv)}. جدد الآن للحفاظ على عضويتك.",
        "Membership Expiring Soon",
        $"Your membership expires on {expiresAt.ToString("MMM dd, yyyy", Inv)}. Renew now to keep your access.");

    public static NotificationText PaymentReceived(decimal amount) => new(
        "تم استلام الدفعة",
        $"تم استلام دفعتك بقيمة {amount.ToString("F2", Inv)} جنيه.",
        "Payment Received",
        $"Your payment of {amount.ToString("F2", Inv)} EGP has been received.");

    public static NotificationText TrainerRated(int stars) => new(
        "تقييم جديد",
        $"حصلت على تقييم {stars.ToString(Inv)} نجوم.",
        "New Rating Received",
        $"You received a {stars.ToString(Inv)}-star rating.");

    public static NotificationText CoachOfTheMonth(int month, int year) => new(
        "مدرب الشهر!",
        $"مبروك! تم اختيارك مدرب الشهر لشهر {month.ToString(Inv)}/{year.ToString(Inv)}!",
        "Coach of the Month!",
        $"Congratulations! You have been selected as Coach of the Month for {month.ToString(Inv)}/{year.ToString(Inv)}!");

    public static NotificationText RemoteWorkoutActivated(string? reason) => reason is not null
        ? new(
            "تم تفعيل التمرين عن بعد",
            $"قام مدربك بتفعيل وضع التمرين عن بعد. السبب: {reason}",
            "Remote Workout Activated",
            $"Your trainer has activated remote workout mode. Reason: {reason}")
        : new(
            "تم تفعيل التمرين عن بعد",
            "قام مدربك بتفعيل وضع التمرين عن بعد لك.",
            "Remote Workout Activated",
            "Your trainer has activated remote workout mode for you.");

    public static NotificationText RemoteWorkoutDeactivated() => new(
        "تم إيقاف التمرين عن بعد",
        "قام مدربك بإيقاف وضع التمرين عن بعد. عدت إلى التمرين داخل الصالة.",
        "Remote Workout Deactivated",
        "Your trainer has deactivated remote workout mode. You are back to in-gym training.");

    /// <summary>The title is the gym's own announcement text and is shown as written.</summary>
    public static NotificationText Announcement(string title) => new(
        title,
        $"إعلان جديد: {title}",
        title,
        $"New announcement: {title}");

    // ---- PT module (Validation_1_PrivateSessions) ----

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
