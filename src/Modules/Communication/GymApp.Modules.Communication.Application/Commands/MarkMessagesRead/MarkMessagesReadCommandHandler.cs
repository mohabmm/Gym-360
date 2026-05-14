using GymApp.Common.Application.Abstractions;
using GymApp.Common.Application.Messaging;
using GymApp.Common.Domain;
using GymApp.Modules.Communication.Application.Abstractions;
using GymApp.Modules.Communication.Domain.Errors;
using GymApp.Modules.Communication.Domain.Repositories;

namespace GymApp.Modules.Communication.Application.Commands.MarkMessagesRead;

internal sealed class MarkMessagesReadCommandHandler : ICommandHandler<MarkMessagesReadCommand>
{
    private readonly IConversationRepository _conversationRepository;
    private readonly ICommunicationUnitOfWork _unitOfWork;
    private readonly IChatNotifier _chatNotifier;

    public MarkMessagesReadCommandHandler(
        IConversationRepository conversationRepository,
        ICommunicationUnitOfWork unitOfWork,
        IChatNotifier chatNotifier)
    {
        _conversationRepository = conversationRepository;
        _unitOfWork = unitOfWork;
        _chatNotifier = chatNotifier;
    }

    public async Task<Result> Handle(MarkMessagesReadCommand request, CancellationToken cancellationToken)
    {
        var conversation = await _conversationRepository.GetByIdAsync(request.ConversationId, cancellationToken);
        if (conversation is null)
            return Result.Failure(CommunicationErrors.ConversationNotFound(request.ConversationId));

        var result = conversation.MarkAsRead(request.UserId);
        if (result.IsFailure)
            return result;

        _conversationRepository.Update(conversation);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        try
        {
            await _chatNotifier.NotifyMessagesReadAsync(
                conversation.Id,
                request.UserId,
                DateTime.UtcNow,
                cancellationToken);
        }
        catch
        {
            // swallow — best effort; REST consumers re-poll
        }

        return Result.Success();
    }
}
