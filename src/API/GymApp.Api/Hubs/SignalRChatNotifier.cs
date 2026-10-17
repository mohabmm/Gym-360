using GymApp.Common.Application.Abstractions;
using Microsoft.AspNetCore.SignalR;

namespace GymApp.Api.Hubs;

internal sealed class SignalRChatNotifier : IChatNotifier
{
    private readonly IHubContext<ChatHub> _hub;

    public SignalRChatNotifier(IHubContext<ChatHub> hub) => _hub = hub;

    public async Task NotifyMessageReceivedAsync(
        ChatMessageNotification message,
        CancellationToken cancellationToken = default)
    {
        var payload = new
        {
            conversationId = message.ConversationId,
            id = message.MessageId,
            senderId = message.SenderId,
            content = message.Content,
            mediaUrl = message.MediaUrl,
            mediaType = message.MediaType,
            sentAt = message.SentAt,
        };

        // Fan out to whoever is currently in the conversation (open thread)
        await _hub.Clients.Group(ChatHub.ConversationGroup(message.ConversationId))
            .SendAsync(ChatHub.MessageReceivedEvent, payload, cancellationToken);

        // Plus a per-user signal so recipients with no conversation open still see
        // their inbox refresh / unread-count bump.
        await _hub.Clients.Group(ChatHub.UserGroup(message.RecipientId))
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
        ConversationCreatedNotification conversation,
        CancellationToken cancellationToken = default)
    {
        var payload = new
        {
            conversationId = conversation.ConversationId,
            initiatorId = conversation.InitiatorId,
            otherParticipantId = conversation.OtherParticipantId,
            gymId = conversation.GymId,
        };
        await _hub.Clients.Group(ChatHub.UserGroup(conversation.OtherParticipantId))
            .SendAsync(ChatHub.ConversationCreatedEvent, payload, cancellationToken);
        await _hub.Clients.Group(ChatHub.UserGroup(conversation.InitiatorId))
            .SendAsync(ChatHub.ConversationCreatedEvent, payload, cancellationToken);
    }
}
