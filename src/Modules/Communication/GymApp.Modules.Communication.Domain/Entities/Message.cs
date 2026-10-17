using GymApp.Common.Domain;
using GymApp.Modules.Communication.Domain.Enums;
using GymApp.Modules.Communication.Domain.ValueObjects;

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

    public static Message Create(MessageDetails details)
    {
        return new Message
        {
            Id = Guid.NewGuid(),
            ConversationId = details.ConversationId,
            SenderId = details.SenderId,
            Content = details.Content,
            MediaUrl = details.MediaUrl,
            MediaType = details.MediaType,
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
