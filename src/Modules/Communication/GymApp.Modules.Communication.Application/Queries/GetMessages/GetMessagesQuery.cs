using GymApp.Common.Application.Messaging;
using GymApp.Modules.Communication.Application.DTOs;

namespace GymApp.Modules.Communication.Application.Queries.GetMessages;

public sealed record GetMessagesQuery(
    Guid ConversationId,
    Guid UserId,
    int Page,
    int PageSize) : IQuery<PagedResponse<MessageDto>>;
