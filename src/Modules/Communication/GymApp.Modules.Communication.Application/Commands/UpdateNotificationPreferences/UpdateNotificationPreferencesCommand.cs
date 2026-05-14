using GymApp.Common.Application.Messaging;
using GymApp.Modules.Communication.Application.DTOs;

namespace GymApp.Modules.Communication.Application.Commands.UpdateNotificationPreferences;

public sealed record UpdateNotificationPreferencesCommand(
    Guid UserId,
    Guid? GymId,
    List<NotificationPreferenceDto> Preferences,
    TimeOnly? QuietHoursStart,
    TimeOnly? QuietHoursEnd) : ICommand;
