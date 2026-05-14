using GymApp.Common.Application.Messaging;
using GymApp.Modules.Communication.Application.DTOs;

namespace GymApp.Modules.Communication.Application.Queries.GetNotificationPreferences;

public sealed record GetNotificationPreferencesQuery(
    Guid UserId,
    Guid? GymId) : IQuery<NotificationPreferencesResponse>;
