using GymApp.Common.Application.Messaging;
using GymApp.Common.Domain;
using GymApp.Modules.Communication.Application.DTOs;
using GymApp.Modules.Communication.Domain.Repositories;

namespace GymApp.Modules.Communication.Application.Queries.GetNotifications;

internal sealed class GetNotificationsQueryHandler : IQueryHandler<GetNotificationsQuery, PagedResponse<NotificationDto>>
{
    private readonly INotificationRepository _notificationRepository;

    public GetNotificationsQueryHandler(INotificationRepository notificationRepository)
    {
        _notificationRepository = notificationRepository;
    }

    public async Task<Result<PagedResponse<NotificationDto>>> Handle(
        GetNotificationsQuery request,
        CancellationToken cancellationToken)
    {
        var notifications = await _notificationRepository.GetByUserAsync(
            request.UserId, request.GymId, request.Page, request.PageSize, cancellationToken);

        var dtos = notifications.Select(n =>
        {
            // Arabic is the stored primary text; English-locale clients get the English copy.
            var (title, body) = n.TextFor(request.Language);
            return new NotificationDto(
                n.Id,
                n.UserId,
                n.GymId,
                title,
                body,
                n.Type.ToString(),
                n.PublicData(),
                n.IsRead,
                n.ReadAt,
                n.CreatedAt);
        }).ToList();

        var totalCount = await _notificationRepository.GetUnreadCountAsync(request.UserId, request.GymId, cancellationToken);

        var response = new PagedResponse<NotificationDto>(
            dtos,
            request.Page,
            request.PageSize,
            dtos.Count);

        return Result.Success(response);
    }
}
