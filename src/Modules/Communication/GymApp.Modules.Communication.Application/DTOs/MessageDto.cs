namespace GymApp.Modules.Communication.Application.DTOs;

public sealed record MessageDto(
    Guid Id,
    Guid ConversationId,
    Guid SenderId,
    string? SenderName,
    string? SenderPhotoUrl,
    string Content,
    string? MediaUrl,
    string MediaType,
    bool IsRead,
    DateTime? ReadAt,
    DateTime SentAt);
