namespace GymApp.Modules.Communication.Application.DTOs;

public sealed record NotificationDto(
    Guid Id,
    Guid UserId,
    Guid? GymId,
    string Title,
    string Body,
    string Type,
    Dictionary<string, string> Data,
    bool IsRead,
    DateTime? ReadAt,
    DateTime CreatedAt);
