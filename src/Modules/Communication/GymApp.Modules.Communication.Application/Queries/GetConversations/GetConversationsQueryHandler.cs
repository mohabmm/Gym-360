using GymApp.Modules.Identity.Shared;
using GymApp.Common.Application.Messaging;
using GymApp.Common.Domain;
using GymApp.Modules.Communication.Application.DTOs;
using GymApp.Modules.Communication.Application.Helpers;
using GymApp.Modules.Communication.Domain.Repositories;

namespace GymApp.Modules.Communication.Application.Queries.GetConversations;

internal sealed class GetConversationsQueryHandler : IQueryHandler<GetConversationsQuery, PagedResponse<ConversationDto>>
{
    private readonly IConversationRepository _conversationRepository;
    private readonly IUserProfileLookup _userProfileLookup;

    public GetConversationsQueryHandler(
        IConversationRepository conversationRepository,
        IUserProfileLookup userProfileLookup)
    {
        _conversationRepository = conversationRepository;
        _userProfileLookup = userProfileLookup;
    }

    public async Task<Result<PagedResponse<ConversationDto>>> Handle(
        GetConversationsQuery request,
        CancellationToken cancellationToken)
    {
        var (conversations, totalCount) = await _conversationRepository.GetByUserAsync(
            request.GymId, request.UserId, request.Page, request.PageSize, cancellationToken);

        if (conversations.Count == 0)
        {
            return Result.Success(new PagedResponse<ConversationDto>(
                new List<ConversationDto>(), request.Page, request.PageSize, totalCount));
        }

        var allParticipantIds = conversations
            .SelectMany(c => c.ParticipantIds)
            .Concat(conversations
                .Select(c => c.Messages.OrderByDescending(m => m.SentAt).FirstOrDefault()?.SenderId)
                .Where(id => id.HasValue)
                .Select(id => id!.Value))
            .Distinct()
            .ToArray();

        var profiles = await _userProfileLookup.GetProfilesAsync(allParticipantIds, cancellationToken);

        var unreadCounts = await _conversationRepository.GetUnreadCountsByConversationAsync(
            conversations.Select(c => c.Id), request.UserId, cancellationToken);

        var dtos = conversations.Select(c =>
        {
            var participants = c.ParticipantIds
                .Select(pid => profiles.TryGetValue(pid, out var p)
                    ? new ConversationParticipantDto(
                        p.Id,
                        UserDisplayName.Resolve(p),
                        p.ProfilePhotoUrl,
                        p.Role)
                    : new ConversationParticipantDto(pid, string.Empty, null, string.Empty))
                .ToList();

            var lastMessage = c.Messages
                .OrderByDescending(m => m.SentAt)
                .FirstOrDefault();

            MessageDto? lastMessageDto = null;
            if (lastMessage is not null)
            {
                profiles.TryGetValue(lastMessage.SenderId, out var sender);
                lastMessageDto = new MessageDto(
                    lastMessage.Id,
                    lastMessage.ConversationId,
                    lastMessage.SenderId,
                    sender is null ? null : UserDisplayName.Resolve(sender),
                    sender?.ProfilePhotoUrl,
                    lastMessage.Content,
                    lastMessage.MediaUrl,
                    lastMessage.MediaType.ToString(),
                    lastMessage.IsRead,
                    lastMessage.ReadAt,
                    lastMessage.SentAt);
            }

            unreadCounts.TryGetValue(c.Id, out var unread);

            return new ConversationDto(
                c.Id,
                c.GymId,
                c.Type.ToString(),
                participants,
                unread,
                c.CreatedAt,
                c.UpdatedAt,
                c.LastMessageAt,
                lastMessageDto);
        }).ToList();

        var response = new PagedResponse<ConversationDto>(
            dtos,
            request.Page,
            request.PageSize,
            totalCount);

        return Result.Success(response);
    }
}
