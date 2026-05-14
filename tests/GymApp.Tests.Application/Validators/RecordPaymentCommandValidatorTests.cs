using FluentValidation.TestHelper;
using GymApp.Modules.Billing.Application.Commands.Payments;

namespace GymApp.Tests.Application.Validators;

public class RecordPaymentCommandValidatorTests
{
    private readonly RecordPaymentCommandValidator _validator = new();

    private static RecordPaymentCommand CreateValidCommand() =>
        new(
            GymId: Guid.NewGuid(),
            MemberId: Guid.NewGuid(),
            Amount: 500m,
            PaymentMethod: "Cash",
            Purpose: "Membership",
            ReferenceId: null,
            RecordedBy: Guid.NewGuid());

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
    public void Validate_EmptyMemberId_HasValidationError()
    {
        var command = CreateValidCommand() with { MemberId = Guid.Empty };
        var result = _validator.TestValidate(command);
        result.ShouldHaveValidationErrorFor(x => x.MemberId);
    }

    [Fact]
    public void Validate_ZeroAmount_HasValidationError()
    {
        var command = CreateValidCommand() with { Amount = 0 };
        var result = _validator.TestValidate(command);
        result.ShouldHaveValidationErrorFor(x => x.Amount);
    }

    [Fact]
    public void Validate_NegativeAmount_HasValidationError()
    {
        var command = CreateValidCommand() with { Amount = -100m };
        var result = _validator.TestValidate(command);
        result.ShouldHaveValidationErrorFor(x => x.Amount);
    }

    [Fact]
    public void Validate_EmptyPaymentMethod_HasValidationError()
    {
        var command = CreateValidCommand() with { PaymentMethod = "" };
        var result = _validator.TestValidate(command);
        result.ShouldHaveValidationErrorFor(x => x.PaymentMethod);
    }

    [Fact]
    public void Validate_EmptyPurpose_HasValidationError()
    {
        var command = CreateValidCommand() with { Purpose = "" };
        var result = _validator.TestValidate(command);
        result.ShouldHaveValidationErrorFor(x => x.Purpose);
    }

    [Fact]
    public void Validate_EmptyRecordedBy_HasValidationError()
    {
        var command = CreateValidCommand() with { RecordedBy = Guid.Empty };
        var result = _validator.TestValidate(command);
        result.ShouldHaveValidationErrorFor(x => x.RecordedBy);
    }
}
