using FluentValidation.TestHelper;
using GymApp.Modules.Scheduling.Application.Commands.BookSession;

namespace GymApp.Tests.Application.Validators;

public class BookSessionCommandValidatorTests
{
    private readonly BookSessionCommandValidator _validator = new();

    private static BookSessionCommand CreateValidCommand() =>
        new(
            GymId: Guid.NewGuid(),
            TrainerId: Guid.NewGuid(),
            MemberId: Guid.NewGuid(),
            Date: DateOnly.FromDateTime(DateTime.UtcNow.AddDays(1)),
            Time: new TimeOnly(10, 0),
            DurationMinutes: 60,
            SessionType: "OneOnOne",
            Focus: "General");

    [Fact]
    public void Validate_ValidCommand_PassesValidation()
    {
        var result = _validator.TestValidate(CreateValidCommand());
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Validate_EmptyGymId_HasValidationError()
    {
        var command = CreateValidCommand() with { GymId = Guid.Empty };
        var result = _validator.TestValidate(command);
        result.ShouldHaveValidationErrorFor(x => x.GymId);
    }

    [Fact]
    public void Validate_EmptyTrainerId_HasValidationError()
    {
        var command = CreateValidCommand() with { TrainerId = Guid.Empty };
        var result = _validator.TestValidate(command);
        result.ShouldHaveValidationErrorFor(x => x.TrainerId);
    }

    [Fact]
    public void Validate_EmptyMemberId_HasValidationError()
    {
        var command = CreateValidCommand() with { MemberId = Guid.Empty };
        var result = _validator.TestValidate(command);
        result.ShouldHaveValidationErrorFor(x => x.MemberId);
    }

    [Fact]
    public void Validate_DurationTooShort_HasValidationError()
    {
        var command = CreateValidCommand() with { DurationMinutes = 10 };
        var result = _validator.TestValidate(command);
        result.ShouldHaveValidationErrorFor(x => x.DurationMinutes)
            .WithErrorMessage("Duration must be between 15 and 180 minutes.");
    }

    [Fact]
    public void Validate_DurationTooLong_HasValidationError()
    {
        var command = CreateValidCommand() with { DurationMinutes = 200 };
        var result = _validator.TestValidate(command);
        result.ShouldHaveValidationErrorFor(x => x.DurationMinutes);
    }

    [Fact]
    public void Validate_EmptySessionType_HasValidationError()
    {
        var command = CreateValidCommand() with { SessionType = "" };
        var result = _validator.TestValidate(command);
        result.ShouldHaveValidationErrorFor(x => x.SessionType);
    }
}
