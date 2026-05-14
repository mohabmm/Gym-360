using GymApp.Common.Domain;
using GymApp.Modules.Communication.Domain.Enums;
using GymApp.Modules.Communication.Domain.Errors;
using GymApp.Modules.Communication.Domain.Events;

namespace GymApp.Modules.Communication.Domain.Entities;

public sealed class Conversation : Entity
{
    private readonly List<Guid> _participantIds = new();
    private readonly List<Message> _messages = new();

    private Conversation() { }

    public Guid GymId { get; private set; }
    public ConversationType Type { get; private set; }
    public IReadOnlyList<Guid> ParticipantIds => _participantIds.AsReadOnly();
    public IReadOnlyList<Message> Messages => _messages.AsReadOnly();
    public DateTime CreatedAt { get; private set; }
    public DateTime UpdatedAt { get; private set; }
    public DateTime? LastMessageAt { get; private set; }

    public static Conversation Create(
        Guid gymId,
        ConversationType type,
        List<Guid> participantIds)
    {
        var conversation = new Conversation
        {
            Id = Guid.NewGuid(),
            GymId = gymId,
            Type = type,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        conversation._participantIds.AddRange(participantIds);

        conversation.RaiseDomainEvent(new ConversationCreatedDomainEvent(conversation.Id, gymId));

        return conversation;
    }

    public Result<Message> SendMessage(Guid senderId, string content, string? mediaUrl = null, MediaType mediaType = MediaType.None)
    {
        if (!_participantIds.Contains(senderId))
            return Result.Failure<Message>(CommunicationErrors.NotParticipant);

        if (content.Length > 4000)
            return Result.Failure<Message>(CommunicationErrors.MessageTooLong);

        if (mediaUrl is not null && mediaType == MediaType.None)
            return Result.Failure<Message>(CommunicationErrors.InvalidMediaType);

        var message = Message.Create(Id, senderId, content, mediaUrl, mediaType);
        _messages.Add(message);
        LastMessageAt = message.SentAt;
        UpdatedAt = DateTime.UtcNow;

        var receiverId = _participantIds.FirstOrDefault(p => p != senderId);
        RaiseDomainEvent(new MessageSentDomainEvent(Id, senderId, receiverId));

        return Result.Success(message);
    }

    public Result MarkAsRead(Guid userId)
    {
        if (!_participantIds.Contains(userId))
            return Result.Failure(CommunicationErrors.NotParticipant);

        foreach (var message in _messages.Where(m => m.SenderId != userId && !m.IsRead))
        {
            message.MarkAsRead();
        }

        UpdatedAt = DateTime.UtcNow;

        return Result.Success();
    }

    public bool IsParticipant(Guid userId) => _participantIds.Contains(userId);
}
