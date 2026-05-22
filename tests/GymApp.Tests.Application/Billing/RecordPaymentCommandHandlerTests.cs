using GymApp.Modules.Billing.Shared;
using GymApp.Common.Application.Errors;
using GymApp.Common.Domain;
using GymApp.Modules.Billing.Application.Commands.Payments;
using GymApp.Modules.Billing.Domain.Entities;
using GymApp.Modules.Billing.Domain.Repositories;
using GymApp.Modules.Billing.Application.Abstractions;

namespace GymApp.Tests.Application.Billing;

public class RecordPaymentCommandHandlerTests
{
    private readonly IPaymentRepository _paymentRepository = Substitute.For<IPaymentRepository>();
    private readonly ISessionPackageRepository _sessionPackageRepository = Substitute.For<ISessionPackageRepository>();
    private readonly IGymPlanService _gymPlanService = Substitute.For<IGymPlanService>();
    private readonly IBillingUnitOfWork _unitOfWork = Substitute.For<IBillingUnitOfWork>();
    private readonly RecordPaymentCommandHandler _sut;

    public RecordPaymentCommandHandlerTests()
    {
        _sut = new RecordPaymentCommandHandler(_paymentRepository, _sessionPackageRepository, _gymPlanService, _unitOfWork);
    }

    private static RecordPaymentCommand CreateValidCommand(Guid? gymId = null) =>
        new(
            GymId: gymId ?? Guid.NewGuid(),
            MemberId: Guid.NewGuid(),
            Amount: 500m,
            PaymentMethod: "Cash",
            Purpose: "Membership",
            ReferenceId: null,
            RecordedBy: Guid.NewGuid());

    [Fact]
    public async Task Handle_FeatureEnabled_RecordsPayment()
    {
        // Arrange
        var command = CreateValidCommand();
        _gymPlanService.IsFeatureEnabledAsync(command.GymId, "payment_recording", Arg.Any<CancellationToken>())
            .Returns(true);
        _gymPlanService.GetPlanCodeAsync(command.GymId, Arg.Any<CancellationToken>())
            .Returns("starter");

        // Act
        var result = await _sut.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeEmpty();
        await _paymentRepository.Received(1).AddAsync(Arg.Any<Payment>(), Arg.Any<CancellationToken>());
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_FeatureDisabled_ReturnsFeatureNotAvailable()
    {
        // Arrange
        var command = CreateValidCommand();
        _gymPlanService.IsFeatureEnabledAsync(command.GymId, "payment_recording", Arg.Any<CancellationToken>())
            .Returns(false);
        _gymPlanService.GetPlanCodeAsync(command.GymId, Arg.Any<CancellationToken>())
            .Returns("free");

        // Act
        var result = await _sut.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("PLAN.FEATURE_NOT_AVAILABLE");
        await _paymentRepository.DidNotReceive().AddAsync(Arg.Any<Payment>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_InvalidPaymentMethod_ReturnsFailure()
    {
        // Arrange
        var command = new RecordPaymentCommand(
            GymId: Guid.NewGuid(),
            MemberId: Guid.NewGuid(),
            Amount: 500m,
            PaymentMethod: "Bitcoin",
            Purpose: "Membership",
            ReferenceId: null,
            RecordedBy: Guid.NewGuid());

        _gymPlanService.IsFeatureEnabledAsync(command.GymId, "payment_recording", Arg.Any<CancellationToken>())
            .Returns(true);

        // Act
        var result = await _sut.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Billing.InvalidPaymentMethod");
    }

    [Fact]
    public async Task Handle_InvalidPurpose_ReturnsFailure()
    {
        // Arrange
        var command = new RecordPaymentCommand(
            GymId: Guid.NewGuid(),
            MemberId: Guid.NewGuid(),
            Amount: 500m,
            PaymentMethod: "Cash",
            Purpose: "InvalidPurpose",
            ReferenceId: null,
            RecordedBy: Guid.NewGuid());

        _gymPlanService.IsFeatureEnabledAsync(command.GymId, "payment_recording", Arg.Any<CancellationToken>())
            .Returns(true);

        // Act
        var result = await _sut.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Billing.InvalidPurpose");
    }
}
