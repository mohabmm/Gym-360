using System.Text.Json;
using FluentAssertions;
using GymApp.Modules.Communication.Application.Contracts;
using GymApp.Modules.Communication.Infrastructure.Services;

namespace GymApp.Tests.Integration.Communication;

/// <summary>
/// Locks the FCM v1 wire format: header/data keys must survive the snake_case
/// naming policy verbatim, and Android must target the app's channel.
/// </summary>
public sealed class PushNotificationPayloadTests
{
    private static JsonElement Serialize(PushContent content) =>
        JsonDocument.Parse(PushNotificationService.SerializeMessage("device-token", content))
            .RootElement.GetProperty("message");

    [Fact]
    public void Apns_UsesHyphenatedPriorityHeader_AndDefaultSound()
    {
        var apns = Serialize(new PushContent("t", "b")).GetProperty("apns");

        apns.GetProperty("headers").GetProperty("apns-priority").GetString().Should().Be("10");
        apns.GetProperty("headers").TryGetProperty("apns_priority", out _).Should().BeFalse();
        apns.GetProperty("payload").GetProperty("aps").GetProperty("sound").GetString().Should().Be("default");
    }

    [Fact]
    public void Android_IsHighPriority_OnSharedChannel()
    {
        var android = Serialize(new PushContent("t", "b")).GetProperty("android");

        android.GetProperty("priority").GetString().Should().Be("high");
        android.GetProperty("notification").GetProperty("channel_id").GetString()
            .Should().Be("gym360_default");
    }

    [Fact]
    public void Data_KeepsCallerKeysVerbatim()
    {
        var message = Serialize(new PushContent("عنوان", "body", new() { ["sessionId"] = "42", ["type"] = "session_reminder" }));

        message.GetProperty("token").GetString().Should().Be("device-token");
        message.GetProperty("notification").GetProperty("title").GetString().Should().Be("عنوان");
        message.GetProperty("data").GetProperty("sessionId").GetString().Should().Be("42");
        message.GetProperty("data").GetProperty("type").GetString().Should().Be("session_reminder");
    }

    [Fact]
    public void Data_DefaultsToEmptyObject_WhenNull()
    {
        Serialize(new PushContent("t", "b")).GetProperty("data").EnumerateObject().Should().BeEmpty();
    }
}
