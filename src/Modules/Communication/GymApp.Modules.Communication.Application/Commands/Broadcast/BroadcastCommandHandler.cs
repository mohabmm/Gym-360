using GymApp.Common.Application.Messaging;
using GymApp.Common.Domain;
using GymApp.Modules.Communication.Domain.Entities;
using GymApp.Modules.Communication.Domain.Enums;
using GymApp.Modules.Communication.Domain.Repositories;
using GymApp.Modules.Communication.Application.Abstractions;

namespace GymApp.Modules.Communication.Application.Commands.Broadcast;

internal sealed class BroadcastCommandHandler : ICommandHandler<BroadcastCommand>
{
    private readonly IConversationRepository _conversationRepository;
    private readonly ICommunicationUnitOfWork _unitOfWork;

    public BroadcastCommandHandler(
        IConversationRepository conversationRepository,
        ICommunicationUnitOfWork unitOfWork)
    {
        _conversationRepository = conversationRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result> Handle(BroadcastCommand request, CancellationToken cancellationToken)
    {
        foreach (var recipientId in request.RecipientIds)
        {
            var conversation = await _conversationRepository.GetByParticipantsAsync(
                request.GymId, request.SenderId, recipientId, cancellationToken);

            if (conversation is null)
            {
                conversation = Conversation.Create(
                    request.GymId,
                    ConversationType.Broadcast,
                    new List<Guid> { request.SenderId, recipientId });

                await _conversationRepository.AddAsync(conversation, cancellationToken);
            }

            // Do not call Update(): Message.Id is ValueGeneratedNever, so Update() would mark the
            // freshly-created Message as Modified (key is non-default) and emit an UPDATE against a
            // non-existent row → DbUpdateConcurrencyException. Change tracking handles the add.
            conversation.SendMessage(request.SenderId, request.Content);
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
