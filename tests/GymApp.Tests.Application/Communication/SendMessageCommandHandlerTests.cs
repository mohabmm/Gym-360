using GymApp.Common.Application.Abstractions;
using GymApp.Common.Domain;
using GymApp.Modules.Communication.Application.Commands.SendMessage;
using GymApp.Modules.Communication.Domain.Entities;
using GymApp.Modules.Communication.Domain.Enums;
using GymApp.Modules.Communication.Domain.Errors;
using GymApp.Modules.Communication.Domain.Repositories;
using GymApp.Modules.Communication.Application.Abstractions;

namespace GymApp.Tests.Application.Communication;

public class SendMessageCommandHandlerTests
{
    private readonly IConversationRepository _conversationRepository = Substitute.For<IConversationRepository>();
    private readonly ICommunicationUnitOfWork _unitOfWork = Substitute.For<ICommunicationUnitOfWork>();
    private readonly IChatNotifier _chatNotifier = Substitute.For<IChatNotifier>();
    private readonly SendMessageCommandHandler _sut;

    public SendMessageCommandHandlerTests()
    {
        _sut = new SendMessageCommandHandler(_conversationRepository, _unitOfWork, _chatNotifier);
    }

    [Fact]
    public async Task Handle_ConversationNotFound_ReturnsFailure()
    {
        // Arrange
        var conversationId = Guid.NewGuid();
        _conversationRepository.GetByIdAsync(conversationId, Arg.Any<CancellationToken>())
            .Returns((Conversation?)null);

        var command = new SendMessageCommand(conversationId, Guid.NewGuid(), "Hello", null, null);

        // Act
        var result = await _sut.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Communication.ConversationNotFound");
    }

    [Fact]
    public async Task Handle_ValidMessage_SendsSuccessfully()
    {
        // Arrange
        var conversationId = Guid.NewGuid();
        var senderId = Guid.NewGuid();
        var gymId = Guid.NewGuid();

        // Create a real conversation with the sender as participant
        var conversation = Conversation.Create(gymId, ConversationType.Direct, new List<Guid> { senderId, Guid.NewGuid() });
        _conversationRepository.GetByIdAsync(conversationId, Arg.Any<CancellationToken>())
            .Returns(conversation);

        var command = new SendMessageCommand(conversationId, senderId, "Hello trainer!", null, null);

        // Act
        var result = await _sut.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeEmpty();
        _conversationRepository.DidNotReceive().Update(Arg.Any<Conversation>());
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }
}
