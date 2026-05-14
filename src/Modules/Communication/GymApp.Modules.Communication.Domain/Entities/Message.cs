using GymApp.Common.Domain;
using GymApp.Modules.Communication.Domain.Enums;

namespace GymApp.Modules.Communication.Domain.Entities;

public sealed class Message : Entity
{
    private Message() { }

    public Guid ConversationId { get; private set; }
    public Guid SenderId { get; private set; }
    public string Content { get; private set; } = default!;
    public string? MediaUrl { get; private set; }
    public MediaType MediaType { get; private set; }
    public bool IsRead { get; private set; }
    public DateTime? ReadAt { get; private set; }
    public DateTime SentAt { get; private set; }

    public static Message Create(
        Guid conversationId,
        Guid senderId,
        string content,
        string? mediaUrl = null,
        MediaType mediaType = MediaType.None)
    {
        return new Message
        {
            Id = Guid.NewGuid(),
            ConversationId = conversationId,
            SenderId = senderId,
            Content = content,
            MediaUrl = mediaUrl,
            MediaType = mediaType,
            IsRead = false,
            SentAt = DateTime.UtcNow
        };
    }

    public void MarkAsRead()
    {
        if (IsRead) return;

        IsRead = true;
        ReadAt = DateTime.UtcNow;
    }
}
