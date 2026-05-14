using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace GymApp.Api.Hubs;

/// <summary>
/// SignalR hub for real-time conversation messaging. Clients join one group per
/// conversation they have open and receive <c>message_received</c> / <c>messages_read</c>
/// events when other participants send or read messages. Server-authoritative — clients
/// cannot push to other clients directly; they must POST through the REST API which
/// in turn invokes <c>IChatNotifier</c> to fan out via this hub.
/// </summary>
[Authorize]
public sealed class ChatHub : Hub
{
    public const string HubPath = "/hubs/chat";
    public const string MessageReceivedEvent = "message_received";
    public const string MessagesReadEvent = "messages_read";
    public const string ConversationCreatedEvent = "conversation_created";

    public static string ConversationGroup(Guid conversationId) => $"conv:{conversationId}";
    public static string UserGroup(Guid userId) => $"user:{userId}";

    public override async Task OnConnectedAsync()
    {
        // Auto-join the per-user group so server can push events targeted at this user
        // (e.g. unread-count bump on a conversation they don't currently have open).
        if (Guid.TryParse(Context.User?.FindFirstValue("sub")
                ?? Context.User?.FindFirstValue(ClaimTypes.NameIdentifier),
            out var userId))
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, UserGroup(userId));
        }
        await base.OnConnectedAsync();
    }

    /// <summary>Subscribe a client connection to a conversation group for real-time messages.</summary>
    public Task JoinConversation(Guid conversationId) =>
        Groups.AddToGroupAsync(Context.ConnectionId, ConversationGroup(conversationId));

    public Task LeaveConversation(Guid conversationId) =>
        Groups.RemoveFromGroupAsync(Context.ConnectionId, ConversationGroup(conversationId));
}
