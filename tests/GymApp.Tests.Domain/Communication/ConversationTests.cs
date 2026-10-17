using GymApp.Common.Domain;
using GymApp.Modules.Communication.Domain.Entities;
using GymApp.Modules.Communication.Domain.Enums;
using GymApp.Modules.Communication.Domain.Events;
using GymApp.Modules.Communication.Domain.ValueObjects;

namespace GymApp.Tests.Domain.Communication;

public sealed class ConversationTests
{
    private static readonly Guid User1Id = Guid.NewGuid();
    private static readonly Guid User2Id = Guid.NewGuid();
    private static readonly Guid GymId = Guid.NewGuid();

    private static Conversation CreateDirectConversation()
    {
        return Conversation.Create(GymId, ConversationType.Direct, new List<Guid> { User1Id, User2Id });
    }

    [Fact]
    public void Create_ShouldSetPropertiesCorrectly()
    {
        var conversation = Conversation.Create(
            GymId, ConversationType.Direct, new List<Guid> { User1Id, User2Id });

        conversation.Id.Should().NotBeEmpty();
        conversation.GymId.Should().Be(GymId);
        conversation.Type.Should().Be(ConversationType.Direct);
        conversation.ParticipantIds.Should().BeEquivalentTo(new[] { User1Id, User2Id });
        conversation.Messages.Should().BeEmpty();
        conversation.LastMessageAt.Should().BeNull();
    }

    [Fact]
    public void Create_ShouldRaiseConversationCreatedDomainEvent()
    {
        var conversation = CreateDirectConversation();

        conversation.GetDomainEvents().Should().ContainSingle()
            .Which.Should().BeOfType<ConversationCreatedDomainEvent>();
    }

    [Fact]
    public void SendMessage_ByParticipant_ShouldAddMessage()
    {
        var conversation = CreateDirectConversation();
        conversation.ClearDomainEvents();

        var result = conversation.SendMessage(User1Id, "Hello!");

        result.IsSuccess.Should().BeTrue();
        conversation.Messages.Should().ContainSingle();
        var message = result.Value;
        message.SenderId.Should().Be(User1Id);
        message.Content.Should().Be("Hello!");
        message.IsRead.Should().BeFalse();
        conversation.LastMessageAt.Should().NotBeNull();
        conversation.GetDomainEvents().Should().ContainSingle()
            .Which.Should().BeOfType<MessageSentDomainEvent>();
    }

    [Fact]
    public void SendMessage_ByNonParticipant_ShouldFail()
    {
        var conversation = CreateDirectConversation();
        var outsider = Guid.NewGuid();

        var result = conversation.SendMessage(outsider, "Hi");

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Communication.NotParticipant");
    }

    [Fact]
    public void SendMessage_TooLong_ShouldFail()
    {
        var conversation = CreateDirectConversation();
        var longMessage = new string('x', 4001);

        var result = conversation.SendMessage(User1Id, longMessage);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Communication.MessageTooLong");
    }

    [Fact]
    public void SendMessage_ExactlyMaxLength_ShouldSucceed()
    {
        var conversation = CreateDirectConversation();
        var maxMessage = new string('x', 4000);

        var result = conversation.SendMessage(User1Id, maxMessage);

        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public void SendMessage_WithMediaUrlButNoType_ShouldFail()
    {
        var conversation = CreateDirectConversation();

        var result = conversation.SendMessage(User1Id, "Photo", "http://photo.jpg", MediaType.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Communication.InvalidMediaType");
    }

    [Fact]
    public void SendMessage_WithMediaUrlAndType_ShouldSucceed()
    {
        var conversation = CreateDirectConversation();

        var result = conversation.SendMessage(User1Id, "Photo", "http://photo.jpg", MediaType.Image);

        result.IsSuccess.Should().BeTrue();
        result.Value.MediaUrl.Should().Be("http://photo.jpg");
        result.Value.MediaType.Should().Be(MediaType.Image);
    }

    [Fact]
    public void MarkAsRead_ShouldMarkOtherUsersMessagesAsRead()
    {
        var conversation = CreateDirectConversation();
        conversation.SendMessage(User1Id, "Message 1");
        conversation.SendMessage(User1Id, "Message 2");

        var result = conversation.MarkAsRead(User2Id);

        result.IsSuccess.Should().BeTrue();
        conversation.Messages.Should().AllSatisfy(m => m.IsRead.Should().BeTrue());
    }

    [Fact]
    public void MarkAsRead_ShouldNotMarkOwnMessagesAsRead()
    {
        var conversation = CreateDirectConversation();
        conversation.SendMessage(User1Id, "My message");
        conversation.SendMessage(User2Id, "Their message");

        conversation.MarkAsRead(User1Id);

        // User1's own message stays unread from User1's perspective (not marked because sender == user)
        // User2's message gets marked as read
        var user1Message = conversation.Messages.First(m => m.SenderId == User1Id);
        var user2Message = conversation.Messages.First(m => m.SenderId == User2Id);

        user1Message.IsRead.Should().BeFalse();
        user2Message.IsRead.Should().BeTrue();
    }

    [Fact]
    public void MarkAsRead_ByNonParticipant_ShouldFail()
    {
        var conversation = CreateDirectConversation();

        var result = conversation.MarkAsRead(Guid.NewGuid());

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Communication.NotParticipant");
    }

    [Fact]
    public void IsParticipant_ShouldReturnCorrectly()
    {
        var conversation = CreateDirectConversation();

        conversation.IsParticipant(User1Id).Should().BeTrue();
        conversation.IsParticipant(User2Id).Should().BeTrue();
        conversation.IsParticipant(Guid.NewGuid()).Should().BeFalse();
    }
}

public sealed class MessageTests
{
    [Fact]
    public void Create_ShouldSetPropertiesCorrectly()
    {
        var conversationId = Guid.NewGuid();
        var senderId = Guid.NewGuid();

        var message = NewMessage(conversationId, senderId, "Hello");

        message.Id.Should().NotBeEmpty();
        message.ConversationId.Should().Be(conversationId);
        message.SenderId.Should().Be(senderId);
        message.Content.Should().Be("Hello");
        message.IsRead.Should().BeFalse();
        message.ReadAt.Should().BeNull();
        message.MediaType.Should().Be(MediaType.None);
    }

    [Fact]
    public void MarkAsRead_ShouldSetIsReadAndReadAt()
    {
        var message = NewMessage(Guid.NewGuid(), Guid.NewGuid(), "Test");

        message.MarkAsRead();

        message.IsRead.Should().BeTrue();
        message.ReadAt.Should().NotBeNull();
    }

    [Fact]
    public void MarkAsRead_WhenAlreadyRead_ShouldNotUpdateReadAt()
    {
        var message = NewMessage(Guid.NewGuid(), Guid.NewGuid(), "Test");
        message.MarkAsRead();
        var firstReadAt = message.ReadAt;

        message.MarkAsRead();

        message.ReadAt.Should().Be(firstReadAt);
    }

    private static Message NewMessage(Guid conversationId, Guid senderId, string content) =>
        Message.Create(new MessageDetails
        {
            ConversationId = conversationId,
            SenderId = senderId,
            Content = content
        });
}
