using System.Net;
using System.Net.Http.Json;
using GymApp.Tests.Functional.Abstractions;

namespace GymApp.Tests.Functional.Communication;

public sealed class CommunicationFunctionalTests : BaseFunctionalTest
{
    public CommunicationFunctionalTests(FunctionalTestWebAppFactory factory)
        : base(factory) { }

    #region Conversations

    [Fact]
    public async Task CreateConversation_Authenticated_ProcessesRequest()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var client = CreateTraineeClient(userId);

        // Act
        var response = await client.PostAsJsonAsync("api/v1/communication/conversations", new
        {
            GymId = Guid.NewGuid(),
            InitiatorId = userId,
            ParticipantId = Guid.NewGuid(),
            Type = "direct"
        });

        // Assert
        response.StatusCode.Should().NotBe(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task CreateConversation_NoToken_Returns401()
    {
        // Act
        var response = await AnonymousClient.PostAsJsonAsync("api/v1/communication/conversations", new
        {
            GymId = Guid.NewGuid(),
            InitiatorId = Guid.NewGuid(),
            ParticipantId = Guid.NewGuid(),
            Type = "direct"
        });

        // Assert
        AssertStatusCode(response, HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetConversations_Authenticated_ProcessesRequest()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var client = CreateTraineeClient(userId);

        // Act
        var response = await client.GetAsync(
            $"api/v1/communication/conversations?userId={userId}&gymId={Guid.NewGuid()}");

        // Assert
        response.StatusCode.Should().NotBe(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetConversations_NoToken_Returns401()
    {
        // Act
        var response = await AnonymousClient.GetAsync(
            $"api/v1/communication/conversations?userId={Guid.NewGuid()}&gymId={Guid.NewGuid()}");

        // Assert
        AssertStatusCode(response, HttpStatusCode.Unauthorized);
    }

    #endregion

    #region Messages

    [Fact]
    public async Task SendMessage_Authenticated_ProcessesRequest()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var client = CreateTraineeClient(userId);
        var conversationId = Guid.NewGuid();

        // Act
        var response = await client.PostAsJsonAsync(
            $"api/v1/communication/conversations/{conversationId}/messages",
            new
            {
                ConversationId = conversationId,
                SenderId = userId,
                Content = "Hello, trainer!",
                MessageType = "text"
            });

        // Assert
        response.StatusCode.Should().NotBe(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task SendMessage_NoToken_Returns401()
    {
        // Arrange
        var conversationId = Guid.NewGuid();

        // Act
        var response = await AnonymousClient.PostAsJsonAsync(
            $"api/v1/communication/conversations/{conversationId}/messages",
            new { ConversationId = conversationId, SenderId = Guid.NewGuid(), Content = "Test", MessageType = "text" });

        // Assert
        AssertStatusCode(response, HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetMessages_Authenticated_ProcessesRequest()
    {
        // Arrange
        var client = CreateTraineeClient();
        var conversationId = Guid.NewGuid();

        // Act
        var response = await client.GetAsync(
            $"api/v1/communication/conversations/{conversationId}/messages?page=1&pageSize=20");

        // Assert
        response.StatusCode.Should().NotBe(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetMessages_NoToken_Returns401()
    {
        // Act
        var response = await AnonymousClient.GetAsync(
            $"api/v1/communication/conversations/{Guid.NewGuid()}/messages?page=1&pageSize=20");

        // Assert
        AssertStatusCode(response, HttpStatusCode.Unauthorized);
    }

    #endregion

    #region Notifications

    [Fact]
    public async Task GetNotifications_Authenticated_ProcessesRequest()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var client = CreateTraineeClient(userId);

        // Act
        var response = await client.GetAsync(
            $"api/v1/communication/notifications?userId={userId}");

        // Assert
        response.StatusCode.Should().NotBe(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetNotifications_NoToken_Returns401()
    {
        // Act
        var response = await AnonymousClient.GetAsync(
            $"api/v1/communication/notifications?userId={Guid.NewGuid()}");

        // Assert
        AssertStatusCode(response, HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetUnreadCount_Authenticated_ProcessesRequest()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var client = CreateTraineeClient(userId);

        // Act
        var response = await client.GetAsync(
            $"api/v1/communication/notifications/unread-count?userId={userId}");

        // Assert
        response.StatusCode.Should().NotBe(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task MarkNotificationRead_Authenticated_ProcessesRequest()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var client = CreateTraineeClient(userId);

        // Act
        var response = await client.PostAsJsonAsync(
            $"api/v1/communication/notifications/{Guid.NewGuid()}/read",
            new { UserId = userId });

        // Assert
        response.StatusCode.Should().NotBe(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task MarkAllRead_Authenticated_ProcessesRequest()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var client = CreateTraineeClient(userId);

        // Act
        var response = await client.PostAsJsonAsync(
            "api/v1/communication/notifications/read-all",
            new { UserId = userId });

        // Assert
        response.StatusCode.Should().NotBe(HttpStatusCode.Unauthorized);
    }

    #endregion

    #region Notification Preferences

    [Fact]
    public async Task GetPreferences_Authenticated_ProcessesRequest()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var client = CreateTraineeClient(userId);

        // Act
        var response = await client.GetAsync(
            $"api/v1/communication/notifications/preferences?userId={userId}");

        // Assert
        response.StatusCode.Should().NotBe(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task UpdatePreferences_Authenticated_ProcessesRequest()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var client = CreateTraineeClient(userId);

        // Act
        var response = await client.PutAsJsonAsync(
            "api/v1/communication/notifications/preferences",
            new
            {
                UserId = userId,
                PushEnabled = true,
                EmailEnabled = false,
                WorkoutReminder = true,
                SessionReminder = true,
                ChatNotifications = true
            });

        // Assert
        response.StatusCode.Should().NotBe(HttpStatusCode.Unauthorized);
    }

    #endregion

    #region Broadcast

    [Fact]
    public async Task Broadcast_AsOwner_ProcessesRequest()
    {
        // Arrange
        var gymId = Guid.NewGuid();
        var client = CreateOwnerClient(gymId: gymId);

        // Act
        var response = await client.PostAsJsonAsync("api/v1/communication/broadcast", new
        {
            GymId = gymId,
            SenderId = Guid.NewGuid(),
            Content = "Gym will be closed tomorrow for maintenance",
            TargetAudience = "all"
        });

        // Assert
        response.StatusCode.Should().NotBe(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Broadcast_NoToken_Returns401()
    {
        // Act
        var response = await AnonymousClient.PostAsJsonAsync("api/v1/communication/broadcast", new
        {
            GymId = Guid.NewGuid(),
            SenderId = Guid.NewGuid(),
            Content = "Test broadcast",
            TargetAudience = "all"
        });

        // Assert
        AssertStatusCode(response, HttpStatusCode.Unauthorized);
    }

    #endregion

    #region Device Token

    [Fact]
    public async Task RegisterDeviceToken_Authenticated_ProcessesRequest()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var client = CreateTraineeClient(userId);

        // Act
        var response = await client.PostAsJsonAsync(
            "api/v1/communication/notifications/device-token",
            new { UserId = userId, Token = $"fcm-{Guid.NewGuid()}", Platform = "android" });

        // Assert
        response.StatusCode.Should().NotBe(HttpStatusCode.Unauthorized);
    }

    #endregion
}
