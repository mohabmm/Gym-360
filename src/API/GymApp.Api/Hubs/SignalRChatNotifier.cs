using GymApp.Common.Application.Abstractions;
using Microsoft.AspNetCore.SignalR;

namespace GymApp.Api.Hubs;

internal sealed class SignalRChatNotifier : IChatNotifier
{
    private readonly IHubContext<ChatHub> _hub;

    public SignalRChatNotifier(IHubContext<ChatHub> hub) => _hub = hub;

    public async Task NotifyMessageReceivedAsync(
        Guid conversationId,
        Guid senderId,
        Guid recipientId,
        Guid messageId,
        string content,
        string? mediaUrl,
        string? mediaType,
        DateTime sentAt,
        CancellationToken cancellationToken = default)
    {
        var payload = new
        {
            conversationId,
            id = messageId,
            senderId,
            content,
            mediaUrl,
            mediaType,
            sentAt,
        };

        // Fan out to whoever is currently in the conversation (open thread)
        await _hub.Clients.Group(ChatHub.ConversationGroup(conversationId))
            .SendAsync(ChatHub.MessageReceivedEvent, payload, cancellationToken);

        // Plus a per-user signal so recipients with no conversation open still see
        // their inbox refresh / unread-count bump.
        await _hub.Clients.Group(ChatHub.UserGroup(recipientId))
            .SendAsync(ChatHub.MessageReceivedEvent, payload, cancellationToken);
    }

    public async Task NotifyMessagesReadAsync(
        Guid conversationId,
        Guid readerId,
        DateTime readAt,
        CancellationToken cancellationToken = default)
    {
        var payload = new { conversationId, readerId, readAt };
        await _hub.Clients.Group(ChatHub.ConversationGroup(conversationId))
            .SendAsync(ChatHub.MessagesReadEvent, payload, cancellationToken);
    }

    public async Task NotifyConversationCreatedAsync(
        Guid conversationId,
        Guid initiatorId,
        Guid otherParticipantId,
        Guid gymId,
        CancellationToken cancellationToken = default)
    {
        var payload = new { conversationId, initiatorId, otherParticipantId, gymId };
        await _hub.Clients.Group(ChatHub.UserGroup(otherParticipantId))
            .SendAsync(ChatHub.ConversationCreatedEvent, payload, cancellationToken);
        await _hub.Clients.Group(ChatHub.UserGroup(initiatorId))
            .SendAsync(ChatHub.ConversationCreatedEvent, payload, cancellationToken);
    }
}
