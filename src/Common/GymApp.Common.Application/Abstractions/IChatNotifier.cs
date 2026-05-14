namespace GymApp.Common.Application.Abstractions;

/// <summary>
/// Cross-module hook for real-time chat fan-out. Communication.Application command
/// handlers call this after a successful persist; the SignalR-backed implementation
/// in the API host pushes events to subscribed clients via ChatHub.
/// Implementations must be tolerant of missing connections (no-op when no listener).
/// </summary>
public interface IChatNotifier
{
    Task NotifyMessageReceivedAsync(
        Guid conversationId,
        Guid senderId,
        Guid recipientId,
        Guid messageId,
        string content,
        string? mediaUrl,
        string? mediaType,
        DateTime sentAt,
        CancellationToken cancellationToken = default);

    Task NotifyMessagesReadAsync(
        Guid conversationId,
        Guid readerId,
        DateTime readAt,
        CancellationToken cancellationToken = default);

    Task NotifyConversationCreatedAsync(
        Guid conversationId,
        Guid initiatorId,
        Guid otherParticipantId,
        Guid gymId,
        CancellationToken cancellationToken = default);
}
