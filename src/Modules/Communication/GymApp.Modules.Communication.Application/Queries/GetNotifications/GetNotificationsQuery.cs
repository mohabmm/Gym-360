using GymApp.Common.Application.Messaging;
using GymApp.Modules.Communication.Application.DTOs;

namespace GymApp.Modules.Communication.Application.Queries.GetNotifications;

public sealed record GetNotificationsQuery(
    Guid UserId,
    Guid? GymId,
    int Page,
    int PageSize) : IQuery<PagedResponse<NotificationDto>>;
