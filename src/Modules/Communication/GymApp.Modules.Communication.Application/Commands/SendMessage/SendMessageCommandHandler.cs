using GymApp.Common.Application.Abstractions;
using GymApp.Common.Application.Messaging;
using GymApp.Common.Domain;
using GymApp.Modules.Communication.Application.Abstractions;
using GymApp.Modules.Communication.Domain.Enums;
using GymApp.Modules.Communication.Domain.Errors;
using GymApp.Modules.Communication.Domain.Repositories;

namespace GymApp.Modules.Communication.Application.Commands.SendMessage;

internal sealed class SendMessageCommandHandler : ICommandHandler<SendMessageCommand, Guid>
{
    private readonly IConversationRepository _conversationRepository;
    private readonly ICommunicationUnitOfWork _unitOfWork;
    private readonly IChatNotifier _chatNotifier;

    public SendMessageCommandHandler(
        IConversationRepository conversationRepository,
        ICommunicationUnitOfWork unitOfWork,
        IChatNotifier chatNotifier)
    {
        _conversationRepository = conversationRepository;
        _unitOfWork = unitOfWork;
        _chatNotifier = chatNotifier;
    }

    public async Task<Result<Guid>> Handle(SendMessageCommand request, CancellationToken cancellationToken)
    {
        var conversation = await _conversationRepository.GetByIdAsync(request.ConversationId, cancellationToken);
        if (conversation is null)
            return Result.Failure<Guid>(CommunicationErrors.ConversationNotFound(request.ConversationId));

        var mediaType = request.MediaType is not null
            ? Enum.Parse<MediaType>(request.MediaType, ignoreCase: true)
            : MediaType.None;

        var messageResult = conversation.SendMessage(request.SenderId, request.Content, request.MediaUrl, mediaType);
        if (messageResult.IsFailure)
            return Result.Failure<Guid>(messageResult.Error);

        // Do not call Update(): Message.Id is ValueGeneratedNever, so Update() would mark the new
        // Message as Modified (non-default key) and UPDATE a non-existent row. The conversation is
        // already tracked from GetByIdAsync; change tracking picks up the added Message.
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        var message = messageResult.Value;
        var recipient = conversation.ParticipantIds.FirstOrDefault(p => p != request.SenderId);

        // Best-effort fan-out via SignalR. Failure here doesn't roll back the message persistence.
        try
        {
            await _chatNotifier.NotifyMessageReceivedAsync(
                conversation.Id,
                request.SenderId,
                recipient,
                message.Id,
                message.Content,
                message.MediaUrl,
                message.MediaType.ToString(),
                message.SentAt,
                cancellationToken);
        }
        catch
        {
            // swallow — REST consumers will pick the message up on their next poll/refresh
        }

        return Result.Success(message.Id);
    }
}
