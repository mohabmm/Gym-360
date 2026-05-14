using System.Net;
using System.Net.Http.Json;
using GymApp.Tests.Integration.Abstractions;

namespace GymApp.Tests.Integration.Communication;

public sealed class CommunicationIntegrationTests : BaseIntegrationTest
{
    public CommunicationIntegrationTests(IntegrationTestWebAppFactory factory)
        : base(factory) { }

    #region Conversations

    [Fact]
    public async Task CreateConversation_Authenticated_ProcessesRequest()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var client = CreateTraineeClient(userId);

        var request = new
        {
            GymId = Guid.NewGuid(),
            InitiatorId = userId,
            ParticipantId = Guid.NewGuid(),
            Type = "direct"
        };

        // Act
        var response = await client.PostAsJsonAsync("api/v1/communication/conversations", request);

        // Assert
        response.StatusCode.Should().NotBe(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task CreateConversation_Unauthenticated_ReturnsUnauthorized()
    {
        // Act
        var response = await HttpClient.PostAsJsonAsync("api/v1/communication/conversations", new
        {
            GymId = Guid.NewGuid(),
            InitiatorId = Guid.NewGuid(),
            ParticipantId = Guid.NewGuid(),
            Type = "direct"
        });

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetConversations_Authenticated_ReturnsResponse()
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

    #endregion

    #region Messages

    [Fact]
    public async Task SendMessage_Authenticated_ProcessesRequest()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var client = CreateTraineeClient(userId);
        var conversationId = Guid.NewGuid();

        var request = new
        {
            ConversationId = conversationId,
            SenderId = userId,
            Content = "Hello, I have a question about my workout plan",
            MessageType = "text"
        };

        // Act
        var response = await client.PostAsJsonAsync(
            $"api/v1/communication/conversations/{conversationId}/messages", request);

        // Assert
        response.StatusCode.Should().NotBe(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task SendMessage_Unauthenticated_ReturnsUnauthorized()
    {
        // Arrange
        var conversationId = Guid.NewGuid();

        // Act
        var response = await HttpClient.PostAsJsonAsync(
            $"api/v1/communication/conversations/{conversationId}/messages",
            new { ConversationId = conversationId, SenderId = Guid.NewGuid(), Content = "Test", MessageType = "text" });

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetMessages_Authenticated_ReturnsResponse()
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

    #endregion

    #region Notifications

    [Fact]
    public async Task GetNotifications_Authenticated_ReturnsResponse()
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
    public async Task GetNotifications_Unauthenticated_ReturnsUnauthorized()
    {
        // Act
        var response = await HttpClient.GetAsync(
            $"api/v1/communication/notifications?userId={Guid.NewGuid()}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetUnreadCount_Authenticated_ReturnsResponse()
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
        var notificationId = Guid.NewGuid();

        // Act
        var response = await client.PostAsJsonAsync(
            $"api/v1/communication/notifications/{notificationId}/read",
            new { UserId = userId });

        // Assert
        response.StatusCode.Should().NotBe(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task MarkAllNotificationsRead_Authenticated_ProcessesRequest()
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
    public async Task GetNotificationPreferences_Authenticated_ReturnsResponse()
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
    public async Task UpdateNotificationPreferences_Authenticated_ProcessesRequest()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var client = CreateTraineeClient(userId);

        var request = new
        {
            UserId = userId,
            PushEnabled = true,
            EmailEnabled = false,
            WorkoutReminder = true,
            SessionReminder = true,
            ChatNotifications = true
        };

        // Act
        var response = await client.PutAsJsonAsync(
            "api/v1/communication/notifications/preferences", request);

        // Assert
        response.StatusCode.Should().NotBe(HttpStatusCode.Unauthorized);
    }

    #endregion

    #region Device Token

    [Fact]
    public async Task RegisterDeviceToken_Authenticated_ProcessesRequest()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var client = CreateTraineeClient(userId);

        var request = new
        {
            UserId = userId,
            Token = "fcm-device-token-" + Guid.NewGuid(),
            Platform = "ios"
        };

        // Act
        var response = await client.PostAsJsonAsync(
            "api/v1/communication/notifications/device-token", request);

        // Assert
        response.StatusCode.Should().NotBe(HttpStatusCode.Unauthorized);
    }

    #endregion
}
