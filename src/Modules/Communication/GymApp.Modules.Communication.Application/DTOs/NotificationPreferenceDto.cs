namespace GymApp.Modules.Communication.Application.DTOs;

public sealed record NotificationPreferenceDto(
    string NotificationType,
    bool PushEnabled,
    bool InAppEnabled);
