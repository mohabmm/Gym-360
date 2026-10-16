using GymApp.Modules.Identity.Shared;
using GymApp.Common.Application.Messaging;
using GymApp.Common.Domain;
using GymApp.Modules.Communication.Application.DTOs;
using GymApp.Modules.Communication.Application.Helpers;
using GymApp.Modules.Communication.Domain.Entities;
using GymApp.Modules.Communication.Domain.Repositories;
using GymApp.Modules.Communication.Domain.ValueObjects;

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
            request.GymId, request.UserId, new PageRequest(request.Page, request.PageSize), cancellationToken);

        if (conversations.Count == 0)
        {
            return Result.Success(new PagedResponse<ConversationDto>(
                new List<ConversationDto>(), request.Page, request.PageSize, totalCount));
        }

        var profiles = await _userProfileLookup.GetProfilesAsync(CollectUserIds(conversations), cancellationToken);

        var unreadCounts = await _conversationRepository.GetUnreadCountsByConversationAsync(
            conversations.Select(c => c.Id), request.UserId, cancellationToken);

        var dtos = conversations
            .Select(c => ToDto(c, profiles, unreadCounts))
            .ToList();

        var response = new PagedResponse<ConversationDto>(
            dtos,
            request.Page,
            request.PageSize,
            totalCount);

        return Result.Success(response);
    }

    // Participants plus each conversation's latest sender, so one batched profile lookup covers both.
    private static Guid[] CollectUserIds(List<Conversation> conversations) =>
        conversations
            .SelectMany(c => c.ParticipantIds)
            .Concat(conversations
                .Select(c => LatestMessage(c)?.SenderId)
                .Where(id => id.HasValue)
                .Select(id => id!.Value))
            .Distinct()
            .ToArray();

    private static Message? LatestMessage(Conversation conversation) =>
        conversation.Messages
            .OrderByDescending(m => m.SentAt)
            .FirstOrDefault();

    private static ConversationDto ToDto(
        Conversation c,
        IReadOnlyDictionary<Guid, UserProfileInfo> profiles,
        IReadOnlyDictionary<Guid, int> unreadCounts)
    {
        var participants = c.ParticipantIds
            .Select(pid => ToParticipantDto(pid, profiles))
            .ToList();

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
            ToLastMessageDto(LatestMessage(c), profiles));
    }

    private static ConversationParticipantDto ToParticipantDto(
        Guid participantId,
        IReadOnlyDictionary<Guid, UserProfileInfo> profiles) =>
        profiles.TryGetValue(participantId, out var p)
            ? new ConversationParticipantDto(
                p.Id,
                UserDisplayName.Resolve(p),
                p.ProfilePhotoUrl,
                p.Role)
            : new ConversationParticipantDto(participantId, string.Empty, null, string.Empty);

    private static MessageDto? ToLastMessageDto(
        Message? lastMessage,
        IReadOnlyDictionary<Guid, UserProfileInfo> profiles)
    {
        if (lastMessage is null)
            return null;

        profiles.TryGetValue(lastMessage.SenderId, out var sender);
        return new MessageDto(
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
}
