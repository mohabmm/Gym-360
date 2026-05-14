using GymApp.Common.Domain;

namespace GymApp.Modules.Communication.Domain.Errors;

public static class CommunicationErrors
{
    public static Error ConversationNotFound(Guid id) =>
        new("Communication.ConversationNotFound", $"Conversation with ID '{id}' was not found.");

    public static Error NotificationNotFound(Guid id) =>
        new("Communication.NotificationNotFound", $"Notification with ID '{id}' was not found.");

    public static Error PreferenceNotFound(Guid userId) =>
        new("Communication.PreferenceNotFound", $"Notification preference for user '{userId}' was not found.");

    public static readonly Error NotParticipant =
        new("Communication.NotParticipant", "User is not a participant in this conversation.");

    public static readonly Error MessageTooLong =
        new("Communication.MessageTooLong", "Message content exceeds the maximum allowed length of 4000 characters.");

    public static readonly Error InvalidMediaType =
        new("Communication.InvalidMediaType", "A media URL was provided but the media type is not specified.");

    public static readonly Error ConversationAlreadyExists =
        new("Communication.ConversationAlreadyExists", "A direct conversation between these participants already exists.");
}
