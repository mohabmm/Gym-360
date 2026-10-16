using System.Text.RegularExpressions;
using GymApp.Modules.Communication.Application.Notifications;
using GymApp.Modules.Communication.Application.Queries.GetNotifications;
using GymApp.Modules.Communication.Domain.Entities;
using GymApp.Modules.Communication.Domain.Enums;
using GymApp.Modules.Communication.Domain.Repositories;
using GymApp.Modules.Communication.Domain.ValueObjects;

namespace GymApp.Tests.Application.Communication;

public class NotificationTextsTests
{
    private static readonly Regex Arabic = new(@"\p{IsArabic}");
    private static readonly Regex Latin = new("[A-Za-z]");
    private static readonly DateTime When = new(2026, 10, 12, 21, 0, 0);

    public static IEnumerable<object[]> SystemTexts() => new[]
    {
        new object[] { NotificationTexts.SessionCompletedForTrainer() },
        new object[] { NotificationTexts.SessionCompletedForMember() },
        new object[] { NotificationTexts.SessionBooked(When) },
        new object[] { NotificationTexts.SessionPackageLow(2) },
        new object[] { NotificationTexts.WorkoutAssigned("Push Day") },
        new object[] { NotificationTexts.StreakMilestone(7, "Workout") },
        new object[] { NotificationTexts.MembershipExpiring(When) },
        new object[] { NotificationTexts.PaymentReceived(150.5m) },
        new object[] { NotificationTexts.TrainerRated(5) },
        new object[] { NotificationTexts.CoachOfTheMonth(10, 2026) },
        new object[] { NotificationTexts.RemoteWorkoutActivated(null) },
        new object[] { NotificationTexts.RemoteWorkoutActivated("travel") },
        new object[] { NotificationTexts.RemoteWorkoutDeactivated() },
    };

    [Theory]
    [MemberData(nameof(SystemTexts))]
    internal void EverySystemNotification_HasArabicPrimaryAndEnglishText(NotificationText text)
    {
        Arabic.IsMatch(text.TitleAr).Should().BeTrue("the primary title is Arabic");
        Arabic.IsMatch(text.BodyAr).Should().BeTrue("the primary body is Arabic");
        Arabic.IsMatch(text.TitleEn).Should().BeFalse();
        Arabic.IsMatch(text.BodyEn).Should().BeFalse();
        Latin.IsMatch(text.TitleEn).Should().BeTrue();
        Latin.IsMatch(text.BodyEn).Should().BeTrue();
    }

    [Fact]
    public void DynamicValues_AppearInBothLanguages()
    {
        var booked = NotificationTexts.SessionBooked(When);
        booked.BodyAr.Should().Contain("12/10/2026").And.Contain("21:00");
        booked.BodyEn.Should().Contain("Oct 12, 2026 at 21:00");

        var payment = NotificationTexts.PaymentReceived(150.5m);
        payment.BodyAr.Should().Contain("150.50");
        payment.BodyEn.Should().Contain("150.50 EGP");

        var workout = NotificationTexts.WorkoutAssigned("Push Day");
        workout.BodyAr.Should().Contain("Push Day");
        workout.BodyEn.Should().Contain("Push Day");
    }

    [Fact]
    public void Announcement_KeepsTheGymsOwnTitle()
    {
        var text = NotificationTexts.Announcement("إغلاق يوم الجمعة");

        text.TitleAr.Should().Be("إغلاق يوم الجمعة");
        text.TitleEn.Should().Be("إغلاق يوم الجمعة");
        text.BodyAr.Should().StartWith("إعلان جديد");
    }

    [Theory]
    [InlineData("ar", "اكتملت الجلسة")]
    [InlineData(null, "اكتملت الجلسة")]
    [InlineData("en-US", "Session Completed")]
    public async Task GetNotifications_ReturnsTextInTheRequestedLanguage(string? language, string expectedTitle)
    {
        var userId = Guid.NewGuid();
        var stored = Notification.CreateLocalized(
            NotificationTexts.SessionCompletedForTrainer(),
            userId, NotificationType.TrainerFeedback, null,
            new Dictionary<string, string> { ["sessionId"] = "s-1" });
        var repository = Substitute.For<INotificationRepository>();
        repository.GetByUserAsync(userId, null, 1, 20, Arg.Any<CancellationToken>())
            .Returns(new List<Notification> { stored });
        var sut = new GetNotificationsQueryHandler(repository);

        var result = await sut.Handle(
            new GetNotificationsQuery(userId, null, 1, 20, language), CancellationToken.None);

        var dto = result.Value.Items.Single();
        dto.Title.Should().Be(expectedTitle);
        dto.Data.Should().ContainKey("sessionId");
        dto.Data.Keys.Should().NotContain(k => k.StartsWith('_'));
    }
}
