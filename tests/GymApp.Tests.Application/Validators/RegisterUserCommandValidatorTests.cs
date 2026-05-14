using FluentValidation.TestHelper;
using GymApp.Modules.Identity.Application.Commands.RegisterUser;

namespace GymApp.Tests.Application.Validators;

public class RegisterUserCommandValidatorTests
{
    private readonly RegisterUserCommandValidator _validator = new();

    [Fact]
    public void Validate_ValidCommand_PassesValidation()
    {
        // Arrange
        var command = new RegisterUserCommand(
            "test@example.com", "Password123!", "John", "Doe", "+201234567890", "Trainee");

        // Act
        var result = _validator.TestValidate(command);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Theory]
    [InlineData("")]
    [InlineData("not-an-email")]
    public void Validate_InvalidEmail_HasValidationError(string email)
    {
        // Arrange
        var command = new RegisterUserCommand(
            email, "Password123!", "John", "Doe", "+201234567890", "Trainee");

        // Act
        var result = _validator.TestValidate(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Email);
    }

    [Fact]
    public void Validate_ShortPassword_HasValidationError()
    {
        // Arrange
        var command = new RegisterUserCommand(
            "test@example.com", "short", "John", "Doe", "+201234567890", "Trainee");

        // Act
        var result = _validator.TestValidate(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Password)
            .WithErrorMessage("Password must be at least 8 characters.");
    }

    [Fact]
    public void Validate_EmptyFirstName_HasValidationError()
    {
        // Arrange
        var command = new RegisterUserCommand(
            "test@example.com", "Password123!", "", "Doe", "+201234567890", "Trainee");

        // Act
        var result = _validator.TestValidate(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.FirstName);
    }

    [Fact]
    public void Validate_EmptyLastName_HasValidationError()
    {
        // Arrange
        var command = new RegisterUserCommand(
            "test@example.com", "Password123!", "John", "", "+201234567890", "Trainee");

        // Act
        var result = _validator.TestValidate(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.LastName);
    }

    [Fact]
    public void Validate_EmptyPhone_ShouldPass()
    {
        // Phone is optional — the handler generates a synthetic placeholder when empty.
        var command = new RegisterUserCommand(
            "test@example.com", "Password123!", "John", "Doe", "", "Trainee");

        var result = _validator.TestValidate(command);

        result.ShouldNotHaveValidationErrorFor(x => x.Phone);
    }

    [Fact]
    public void Validate_TooShortPhone_HasValidationError()
    {
        var command = new RegisterUserCommand(
            "test@example.com", "Password123!", "John", "Doe", "123", "Trainee");

        var result = _validator.TestValidate(command);

        result.ShouldHaveValidationErrorFor(x => x.Phone);
    }

    [Fact]
    public void Validate_EmptyRole_HasValidationError()
    {
        // Arrange
        var command = new RegisterUserCommand(
            "test@example.com", "Password123!", "John", "Doe", "+201234567890", "");

        // Act
        var result = _validator.TestValidate(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Role);
    }

    [Fact]
    public void Validate_FirstNameExceedsMaxLength_HasValidationError()
    {
        // Arrange
        var command = new RegisterUserCommand(
            "test@example.com", "Password123!", new string('A', 101), "Doe", "+201234567890", "Trainee");

        // Act
        var result = _validator.TestValidate(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.FirstName);
    }
}
