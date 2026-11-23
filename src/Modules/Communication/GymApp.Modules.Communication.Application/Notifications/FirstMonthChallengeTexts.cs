using System.Globalization;
using GymApp.Modules.Communication.Domain.ValueObjects;

namespace GymApp.Modules.Communication.Application.Notifications;

/// <summary>mem.first_month_challenge wording, Arabic first (Egyptian, as the gym floor speaks).</summary>
internal static class FirstMonthChallengeTexts
{
    private static readonly CultureInfo Inv = CultureInfo.InvariantCulture;

    public static NotificationText ForTrainee(int count, string rewardAr, string rewardEn) => new(
        "مبروك! كسبت تحدي الشهر الأول 🎉",
        $"خلّصت {count.ToString(Inv)} تمرين في أول شهر. مكافأتك: {rewardAr}. كلّم الاستقبال أو الكابتن عشان تحدد ميعادها.",
        "Congratulations! You won the first-month challenge 🎉",
        $"You finished {count.ToString(Inv)} workouts in your first month. Your reward: {rewardEn}. Ask the desk or your coach to book it.");

    public static NotificationText ForCoach(string memberName, int count) => new(
        $"{memberName} كسب تحدي الشهر الأول",
        $"خلّص {count.ToString(Inv)} تمرين في أول شهر. اتفق معاه على جلسة المتابعة: قياسات جديدة وجدول الشهر الجاي.",
        $"{memberName} won the first-month challenge",
        $"{count.ToString(Inv)} workouts in the first month. Agree on the follow-up session with them: new measurements and next month's plan.");

    public static NotificationText ForDesk(string memberName, string rewardAr, string rewardEn) => new(
        $"{memberName} كسب مكافأة الشهر الأول",
        $"المكافأة: {rewardAr}. علّم عليها \"اتسلمت\" في قائمة المكافآت بعد ما يستلمها.",
        $"{memberName} earned the first-month reward",
        $"Reward: {rewardEn}. Tick \"delivered\" in the rewards list once they have it.");
}
