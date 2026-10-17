using GymApp.Modules.Communication.Domain.Entities;
using GymApp.Modules.Communication.Domain.Enums;
using GymApp.Modules.Communication.Domain.ValueObjects;

namespace GymApp.Tests.Domain.Communication;

public sealed class NotificationLocalizationTests
{
    private static Notification Localized() => Notification.CreateLocalized(
        new NotificationDetails
        {
            UserId = Guid.NewGuid(),
            Type = NotificationType.SessionReminder,
            GymId = Guid.NewGuid(),
            Data = new Dictionary<string, string> { ["sessionId"] = "abc" }
        },
        new NotificationText(
            "اكتملت الجلسة", "اكتملت جلستك التدريبية.",
            "Session Completed", "Your training session has been completed."));

    [Fact]
    public void CreateLocalized_StoresArabicAsPrimaryText()
    {
        var n = Localized();

        n.Title.Should().Be("اكتملت الجلسة");
        n.Body.Should().Be("اكتملت جلستك التدريبية.");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("ar")]
    [InlineData("ar-EG")]
    [InlineData("fr")]
    public void TextFor_NonEnglishLanguage_ReturnsArabic(string? language)
    {
        Localized().TextFor(language).Should().Be(("اكتملت الجلسة", "اكتملت جلستك التدريبية."));
    }

    [Theory]
    [InlineData("en")]
    [InlineData("en-US")]
    [InlineData("EN")]
    public void TextFor_English_ReturnsEnglish(string language)
    {
        Localized().TextFor(language)
            .Should().Be(("Session Completed", "Your training session has been completed."));
    }

    [Fact]
    public void TextFor_English_OnNotificationWithoutTranslation_FallsBackToStoredText()
    {
        var n = Notification.Create(
            new NotificationDetails { UserId = Guid.NewGuid(), Type = NotificationType.Announcement },
            "Custom title",
            "Custom body");

        n.TextFor("en").Should().Be(("Custom title", "Custom body"));
    }

    [Fact]
    public void PublicData_HidesReservedTextKeys_AndKeepsMetadata()
    {
        var data = Localized().PublicData();

        data.Should().ContainKey("sessionId").WhoseValue.Should().Be("abc");
        data.Should().NotContainKey(Notification.TitleEnKey);
        data.Should().NotContainKey(Notification.BodyEnKey);
    }
}
