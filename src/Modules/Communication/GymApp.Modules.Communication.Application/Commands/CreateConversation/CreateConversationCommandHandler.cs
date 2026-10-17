using GymApp.Common.Application.Abstractions;
using GymApp.Common.Application.Messaging;
using GymApp.Common.Domain;
using GymApp.Modules.Communication.Application.Abstractions;
using GymApp.Modules.Communication.Domain.Entities;
using GymApp.Modules.Communication.Domain.Enums;
using GymApp.Modules.Communication.Domain.Repositories;

namespace GymApp.Modules.Communication.Application.Commands.CreateConversation;

internal sealed class CreateConversationCommandHandler : ICommandHandler<CreateConversationCommand, Guid>
{
    private readonly IConversationRepository _conversationRepository;
    private readonly ICommunicationUnitOfWork _unitOfWork;
    private readonly IChatNotifier _chatNotifier;

    public CreateConversationCommandHandler(
        IConversationRepository conversationRepository,
        ICommunicationUnitOfWork unitOfWork,
        IChatNotifier chatNotifier)
    {
        _conversationRepository = conversationRepository;
        _unitOfWork = unitOfWork;
        _chatNotifier = chatNotifier;
    }

    public async Task<Result<Guid>> Handle(CreateConversationCommand request, CancellationToken cancellationToken)
    {
        // Idempotent: if a direct conversation between these two participants already exists in
        // this gym, return its id. The previous behavior was to fail with ConversationAlreadyExists,
        // which forced clients to handle the error and re-fetch — wasteful round-trip and a flaky
        // race when two clients open the chat simultaneously.
        var existing = await _conversationRepository.GetByParticipantsAsync(
            request.GymId, request.Participant1, request.Participant2, cancellationToken);

        if (existing is not null)
            return Result.Success(existing.Id);

        var conversation = Conversation.Create(
            request.GymId,
            ConversationType.Direct,
            new List<Guid> { request.Participant1, request.Participant2 });

        await _conversationRepository.AddAsync(conversation, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        await _chatNotifier.NotifyConversationCreatedAsync(
            new ConversationCreatedNotification
            {
                ConversationId = conversation.Id,
                InitiatorId = request.Participant1,
                OtherParticipantId = request.Participant2,
                GymId = request.GymId
            },
            cancellationToken);

        return Result.Success(conversation.Id);
    }
}
