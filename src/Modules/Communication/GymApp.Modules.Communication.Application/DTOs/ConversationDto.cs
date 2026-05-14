namespace GymApp.Modules.Communication.Application.DTOs;

public sealed record ConversationDto(
    Guid Id,
    Guid GymId,
    string Type,
    List<ConversationParticipantDto> Participants,
    int UnreadCount,
    DateTime CreatedAt,
    DateTime UpdatedAt,
    DateTime? LastMessageAt,
    MessageDto? LastMessage);
