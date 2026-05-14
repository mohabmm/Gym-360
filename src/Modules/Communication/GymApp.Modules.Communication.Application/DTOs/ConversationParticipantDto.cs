namespace GymApp.Modules.Communication.Application.DTOs;

public sealed record ConversationParticipantDto(
    Guid UserId,
    string Name,
    string? PhotoUrl,
    string Role);
