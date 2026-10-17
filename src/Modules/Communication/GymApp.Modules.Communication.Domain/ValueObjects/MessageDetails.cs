using GymApp.Modules.Communication.Domain.Enums;

namespace GymApp.Modules.Communication.Domain.ValueObjects;

/// <summary>A chat message: which conversation, who sent it, and its text/media content.</summary>
public sealed record MessageDetails
{
    public required Guid ConversationId { get; init; }
    public required Guid SenderId { get; init; }
    public required string Content { get; init; }
    public string? MediaUrl { get; init; }
    public MediaType MediaType { get; init; } = MediaType.None;
}
