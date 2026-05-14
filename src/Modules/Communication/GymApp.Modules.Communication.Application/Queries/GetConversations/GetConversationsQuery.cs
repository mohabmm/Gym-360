using GymApp.Common.Application.Messaging;
using GymApp.Modules.Communication.Application.DTOs;

namespace GymApp.Modules.Communication.Application.Queries.GetConversations;

public sealed record GetConversationsQuery(
    Guid UserId,
    Guid GymId,
    int Page,
    int PageSize) : IQuery<PagedResponse<ConversationDto>>;
