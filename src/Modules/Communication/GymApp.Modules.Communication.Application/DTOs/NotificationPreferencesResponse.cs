namespace GymApp.Modules.Communication.Application.DTOs;

public sealed record NotificationPreferencesResponse(
    Guid UserId,
    Guid? GymId,
    List<NotificationPreferenceDto> Preferences,
    TimeOnly? QuietHoursStart,
    TimeOnly? QuietHoursEnd);
