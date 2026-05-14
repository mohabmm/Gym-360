using GymApp.Common.Application.Messaging;
using GymApp.Modules.Communication.Application.DTOs;

namespace GymApp.Modules.Communication.Application.Queries.GetUnreadCount;

public sealed record GetUnreadCountQuery(
    Guid UserId,
    Guid? GymId) : IQuery<UnreadCountResponse>;
