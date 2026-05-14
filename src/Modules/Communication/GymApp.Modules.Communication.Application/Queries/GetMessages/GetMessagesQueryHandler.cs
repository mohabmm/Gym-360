using GymApp.Common.Application.Abstractions;
using GymApp.Common.Application.Messaging;
using GymApp.Common.Domain;
using GymApp.Modules.Communication.Application.DTOs;
using GymApp.Modules.Communication.Application.Helpers;
using GymApp.Modules.Communication.Domain.Errors;
using GymApp.Modules.Communication.Domain.Repositories;

namespace GymApp.Modules.Communication.Application.Queries.GetMessages;

internal sealed class GetMessagesQueryHandler : IQueryHandler<GetMessagesQuery, PagedResponse<MessageDto>>
{
    private readonly IConversationRepository _conversationRepository;
    private readonly IUserProfileLookup _userProfileLookup;

    public GetMessagesQueryHandler(
        IConversationRepository conversationRepository,
        IUserProfileLookup userProfileLookup)
    {
        _conversationRepository = conversationRepository;
        _userProfileLookup = userProfileLookup;
    }

    public async Task<Result<PagedResponse<MessageDto>>> Handle(
        GetMessagesQuery request,
        CancellationToken cancellationToken)
    {
        var conversation = await _conversationRepository.GetByIdAsync(request.ConversationId, cancellationToken);
        if (conversation is null)
            return Result.Failure<PagedResponse<MessageDto>>(
                CommunicationErrors.ConversationNotFound(request.ConversationId));

        if (!conversation.IsParticipant(request.UserId))
            return Result.Failure<PagedResponse<MessageDto>>(CommunicationErrors.NotParticipant);

        var pageMessages = conversation.Messages
            .OrderByDescending(m => m.SentAt)
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .ToList();

        var senderIds = pageMessages.Select(m => m.SenderId).Distinct().ToArray();
        var profiles = senderIds.Length == 0
            ? new Dictionary<Guid, UserProfileInfo>()
            : (await _userProfileLookup.GetProfilesAsync(senderIds, cancellationToken))
                .ToDictionary(kv => kv.Key, kv => kv.Value);

        var messages = pageMessages
            .Select(m =>
            {
                profiles.TryGetValue(m.SenderId, out var sender);
                return new MessageDto(
                    m.Id,
                    m.ConversationId,
                    m.SenderId,
                    sender is null ? null : UserDisplayName.Resolve(sender),
                    sender?.ProfilePhotoUrl,
                    m.Content,
                    m.MediaUrl,
                    m.MediaType.ToString(),
                    m.IsRead,
                    m.ReadAt,
                    m.SentAt);
            })
            .ToList();

        var response = new PagedResponse<MessageDto>(
            messages,
            request.Page,
            request.PageSize,
            conversation.Messages.Count);

        return Result.Success(response);
    }
}
