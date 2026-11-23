using FluentAssertions;
using GymApp.Modules.Training.Domain.Entities;
using GymApp.Modules.Training.Domain.ValueObjects;
using Xunit;

namespace GymApp.Tests.Domain.Training.Plans;

public class FirstMonthChallengeTests
{
    private static readonly DateOnly Joined = new(2026, 11, 1);

    [Fact]
    public void The_window_starts_on_the_join_date_and_falls_back_to_the_first_plan_start()
    {
        ChallengeWindow.For(Joined, Joined.AddDays(5), 30).Should().Be(new ChallengeWindow { Start = Joined, End = new DateOnly(2026, 11, 30) });
        ChallengeWindow.For(null, Joined.AddDays(5), 30)!.Start.Should().Be(Joined.AddDays(5));
        ChallengeWindow.For(null, null, 30).Should().BeNull("no join date and no plan: nothing has started");
    }

    [Fact]
    public void Only_workouts_inside_the_window_that_were_not_voided_count()
    {
        var window = ChallengeWindow.Of(Joined, 30);
        var workouts = new[]
        {
            new ChallengeWorkout(Joined.AddDays(-1), false), // before joining
            new ChallengeWorkout(Joined, false),             // first day counts
            new ChallengeWorkout(Joined.AddDays(29), false), // last day counts
            new ChallengeWorkout(Joined.AddDays(30), false), // day 31 does not
            new ChallengeWorkout(Joined.AddDays(3), true),   // reversed session
        };

        window.Count(workouts).Should().Be(2);
    }

    [Theory]
    [InlineData(-3, 30)] // not started yet: the full window is ahead
    [InlineData(0, 30)]
    [InlineData(29, 1)]
    [InlineData(30, 0)]
    public void Days_left_include_today_and_reach_zero_after_the_window(int dayOffset, int daysLeft)
    {
        var window = ChallengeWindow.Of(Joined, 30);

        window.DaysLeft(Joined.AddDays(dayOffset)).Should().Be(daysLeft);
        window.HasEnded(Joined.AddDays(dayOffset)).Should().Be(daysLeft == 0);
    }

    [Fact]
    public void Settings_default_to_12_workouts_in_30_days_with_the_follow_up_session_reward()
    {
        var settings = TrainingPlanSettings.ForGym(Guid.NewGuid());

        settings.ChallengeTarget.Should().Be(12);
        settings.ChallengeWindowDays.Should().Be(30);
        settings.EffectiveRewardTextAr.Should().Be("جلسة متابعة مع المدرب: قياسات جديدة وجدول الشهر الجاي");
        settings.EffectiveRewardTextEn.Should().Contain("follow-up session");
    }

    [Theory]
    [InlineData(0, 30)]
    [InlineData(61, 30)]
    [InlineData(12, 6)]
    [InlineData(12, 91)]
    public void Out_of_range_challenge_settings_are_refused(int target, int windowDays)
    {
        var settings = TrainingPlanSettings.ForGym(Guid.NewGuid());

        settings.SetChallenge(new ChallengeSettings(target, windowDays, null, null), Guid.NewGuid()).IsFailure.Should().BeTrue();
        settings.ChallengeTarget.Should().Be(12);
    }

    [Fact]
    public void A_gym_can_word_its_own_reward_and_blank_goes_back_to_the_default()
    {
        var settings = TrainingPlanSettings.ForGym(Guid.NewGuid());

        settings.SetChallenge(new ChallengeSettings(10, 21, "  شهر اشتراك مجاني  ", null), Guid.NewGuid()).IsSuccess.Should().BeTrue();
        settings.EffectiveRewardTextAr.Should().Be("شهر اشتراك مجاني");
        settings.EffectiveRewardTextEn.Should().Be("شهر اشتراك مجاني", "English falls back to the gym's own Arabic words");

        settings.SetChallenge(new ChallengeSettings(10, 21, " ", ""), Guid.NewGuid()).IsSuccess.Should().BeTrue();
        settings.EffectiveRewardTextAr.Should().Be(TrainingPlanSettings.DefaultRewardTextAr);
        settings.SetChallenge(new ChallengeSettings(10, 21, new string('x', 301), null), Guid.NewGuid()).IsFailure.Should().BeTrue();
    }

    [Fact]
    public void The_award_keeps_the_reward_as_worded_when_reached_and_the_desk_can_tick_and_untick_delivery()
    {
        var deskId = Guid.NewGuid();
        var award = ChallengeAward.Reach(new ChallengeReach
        {
            GymId = Guid.NewGuid(), TraineeId = Guid.NewGuid(), Window = ChallengeWindow.Of(Joined, 30),
            Target = 12, Count = 12, RewardTextAr = "جلسة متابعة", RewardTextEn = "Follow-up session",
        });

        award.IsDelivered.Should().BeFalse();
        award.SetDelivered(true, deskId);
        award.DeliveredBy.Should().Be(deskId);
        award.DeliveredAt.Should().NotBeNull();

        award.SetDelivered(false, deskId);
        award.IsDelivered.Should().BeFalse();
        award.DeliveredBy.Should().BeNull();
        award.WindowEnd.Should().Be(new DateOnly(2026, 11, 30));
    }
}
