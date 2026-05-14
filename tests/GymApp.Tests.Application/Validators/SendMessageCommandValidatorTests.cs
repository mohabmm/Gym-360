using FluentValidation.TestHelper;
using GymApp.Modules.Communication.Application.Commands.SendMessage;

namespace GymApp.Tests.Application.Validators;

public class SendMessageCommandValidatorTests
{
    private readonly SendMessageCommandValidator _validator = new();

    private static SendMessageCommand CreateValidCommand() =>
        new(
            ConversationId: Guid.NewGuid(),
            SenderId: Guid.NewGuid(),
            Content: "Hello!",
            MediaUrl: null,
            MediaType: null);

    [Fact]
    public void Validate_ValidCommand_PassesValidation()
    {
        var result = _validator.TestValidate(CreateValidCommand());
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Validate_EmptyConversationId_HasValidationError()
    {
        var command = CreateValidCommand() with { ConversationId = Guid.Empty };
        var result = _validator.TestValidate(command);
        result.ShouldHaveValidationErrorFor(x => x.ConversationId);
    }

    [Fact]
    public void Validate_EmptySenderId_HasValidationError()
    {
        var command = CreateValidCommand() with { SenderId = Guid.Empty };
        var result = _validator.TestValidate(command);
        result.ShouldHaveValidationErrorFor(x => x.SenderId);
    }

    [Fact]
    public void Validate_EmptyContent_HasValidationError()
    {
        var command = CreateValidCommand() with { Content = "" };
        var result = _validator.TestValidate(command);
        result.ShouldHaveValidationErrorFor(x => x.Content);
    }

    [Fact]
    public void Validate_ContentExceedsMaxLength_HasValidationError()
    {
        var command = CreateValidCommand() with { Content = new string('A', 4001) };
        var result = _validator.TestValidate(command);
        result.ShouldHaveValidationErrorFor(x => x.Content);
    }

    [Fact]
    public void Validate_ValidMediaType_PassesValidation()
    {
        var command = CreateValidCommand() with { MediaType = "Image" };
        var result = _validator.TestValidate(command);
        result.ShouldNotHaveValidationErrorFor(x => x.MediaType);
    }

    [Fact]
    public void Validate_InvalidMediaType_HasValidationError()
    {
        var command = CreateValidCommand() with { MediaType = "InvalidType" };
        var result = _validator.TestValidate(command);
        result.ShouldHaveValidationErrorFor(x => x.MediaType);
    }
}
