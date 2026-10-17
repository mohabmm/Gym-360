namespace GymApp.Common.Application.Abstractions;

/// <summary>A persisted chat message to fan out via <see cref="IChatNotifier.NotifyMessageReceivedAsync"/>.</summary>
public sealed record ChatMessageNotification
{
    public required Guid ConversationId { get; init; }
    public required Guid SenderId { get; init; }
    public required Guid RecipientId { get; init; }
    public required Guid MessageId { get; init; }
    public required string Content { get; init; }
    public string? MediaUrl { get; init; }
    public string? MediaType { get; init; }
    public required DateTime SentAt { get; init; }
}

/// <summary>A newly created conversation to fan out via <see cref="IChatNotifier.NotifyConversationCreatedAsync"/>.</summary>
public sealed record ConversationCreatedNotification
{
    public required Guid ConversationId { get; init; }
    public required Guid InitiatorId { get; init; }
    public required Guid OtherParticipantId { get; init; }
    public required Guid GymId { get; init; }
}
