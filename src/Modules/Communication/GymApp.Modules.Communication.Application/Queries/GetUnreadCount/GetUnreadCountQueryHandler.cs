using GymApp.Common.Application.Messaging;
using GymApp.Common.Domain;
using GymApp.Modules.Communication.Application.DTOs;
using GymApp.Modules.Communication.Domain.Repositories;

namespace GymApp.Modules.Communication.Application.Queries.GetUnreadCount;

internal sealed class GetUnreadCountQueryHandler : IQueryHandler<GetUnreadCountQuery, UnreadCountResponse>
{
    private readonly INotificationRepository _notificationRepository;

    public GetUnreadCountQueryHandler(INotificationRepository notificationRepository)
    {
        _notificationRepository = notificationRepository;
    }

    public async Task<Result<UnreadCountResponse>> Handle(
        GetUnreadCountQuery request,
        CancellationToken cancellationToken)
    {
        var unreadNotifications = await _notificationRepository.GetUnreadCountAsync(
            request.UserId, request.GymId, cancellationToken);

        // Unread messages count would require a dedicated repo method;
        // for now we return 0 and let Infrastructure provide the real count.
        var response = new UnreadCountResponse(unreadNotifications, UnreadMessages: 0);

        return Result.Success(response);
    }
}
