using GymApp.Common.Domain;
using GymApp.Modules.Billing.Application.Commands.Payments;
using GymApp.Modules.Billing.Domain.Entities;
using GymApp.Modules.Billing.Domain.Enums;
using GymApp.Modules.Billing.Domain.Errors;
using GymApp.Modules.Billing.Domain.Repositories;
using GymApp.Modules.Billing.Domain.ValueObjects;
using GymApp.Modules.Billing.Application.Abstractions;

namespace GymApp.Tests.Application.Billing;

public class VoidPaymentCommandHandlerTests
{
    private readonly IPaymentRepository _paymentRepository = Substitute.For<IPaymentRepository>();
    private readonly IBillingUnitOfWork _unitOfWork = Substitute.For<IBillingUnitOfWork>();
    private readonly VoidPaymentCommandHandler _sut;

    public VoidPaymentCommandHandlerTests()
    {
        _sut = new VoidPaymentCommandHandler(_paymentRepository, _unitOfWork);
    }

    [Fact]
    public async Task Handle_PaymentNotFound_ReturnsFailure()
    {
        // Arrange
        var paymentId = Guid.NewGuid();
        _paymentRepository.GetByIdAsync(paymentId, Arg.Any<CancellationToken>())
            .Returns((Payment?)null);

        var command = new VoidPaymentCommand(paymentId, "Duplicate charge");

        // Act
        var result = await _sut.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be(BillingErrors.PaymentNotFound.Code);
    }

    [Fact]
    public async Task Handle_ValidPayment_VoidsSuccessfully()
    {
        // Arrange -- No refund flow, only void (per Q9)
        var paymentId = Guid.NewGuid();
        var payment = Payment.Create(new PaymentDetails
        {
            GymId = Guid.NewGuid(),
            MemberId = Guid.NewGuid(),
            Amount = Money.Create(500m),
            PaymentMethod = PaymentMethod.Cash,
            Purpose = PaymentPurpose.Membership,
            ReceiptNumber = "RCP-TEST-001",
            RecordedBy = Guid.NewGuid(),
            ReferenceId = null
        });
        _paymentRepository.GetByIdAsync(paymentId, Arg.Any<CancellationToken>())
            .Returns(payment);

        var command = new VoidPaymentCommand(paymentId, "Duplicate charge");

        // Act
        var result = await _sut.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        _paymentRepository.Received(1).Update(payment);
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_AlreadyVoidedPayment_ReturnsFailure()
    {
        // Arrange
        var paymentId = Guid.NewGuid();
        var payment = Payment.Create(new PaymentDetails
        {
            GymId = Guid.NewGuid(),
            MemberId = Guid.NewGuid(),
            Amount = Money.Create(500m),
            PaymentMethod = PaymentMethod.Cash,
            Purpose = PaymentPurpose.Membership,
            ReceiptNumber = "RCP-TEST-002",
            RecordedBy = Guid.NewGuid(),
            ReferenceId = null
        });
        payment.Void("First void"); // Void it once
        _paymentRepository.GetByIdAsync(paymentId, Arg.Any<CancellationToken>())
            .Returns(payment);

        var command = new VoidPaymentCommand(paymentId, "Second void attempt");

        // Act
        var result = await _sut.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
    }
}
