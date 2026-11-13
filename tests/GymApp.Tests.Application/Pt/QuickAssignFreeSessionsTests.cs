using GymApp.Common.Application.FeatureFlags;
using GymApp.Modules.Billing.Application.Abstractions;
using GymApp.Modules.Billing.Application.Commands.SessionPackages;
using GymApp.Modules.Billing.Domain.Entities;
using GymApp.Modules.Billing.Domain.Repositories;

namespace GymApp.Tests.Application.Pt;

/// <summary>
/// Staff add a member with a coach and any number of free (or paid) sessions.
/// </summary>
public class QuickAssignFreeSessionsTests
{
    private readonly ISessionPackageRepository _packages = Substitute.For<ISessionPackageRepository>();
    private readonly IPaymentRepository _payments = Substitute.For<IPaymentRepository>();
    private readonly IFeatureFlagService _flags = Substitute.For<IFeatureFlagService>();
    private readonly IBillingUnitOfWork _uow = Substitute.For<IBillingUnitOfWork>();
    private readonly QuickAssignSessionPackageCommandHandler _sut;
    private readonly QuickAssignSessionPackageCommandValidator _validator = new();

    private static readonly Guid GymId = Guid.NewGuid();

    public QuickAssignFreeSessionsTests()
    {
        _flags.IsEnabledAsync(GymId, Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(ci => ci.ArgAt<string>(1) != FeatureKeys.PtAutoExpiry);
        _sut = new QuickAssignSessionPackageCommandHandler(_packages, _payments, _flags, _uow);
    }

    private static QuickAssignSessionPackageCommand Package(int sessions, decimal? price = null, string? method = null) =>
        new(GymId, Guid.NewGuid(), Guid.NewGuid(), sessions, Guid.NewGuid(), TotalPrice: price, PaymentMethod: method);

    [Theory]
    [InlineData(0, false)]
    [InlineData(1, true)]
    [InlineData(7, true)]
    [InlineData(100, true)]
    [InlineData(101, false)]
    public void Validator_accepts_any_session_count_from_one_to_one_hundred(int sessions, bool valid)
    {
        _validator.Validate(Package(sessions)).IsValid.Should().Be(valid);
    }

    [Fact]
    public async Task Free_package_keeps_the_coach_and_count_and_records_no_payment()
    {
        SessionPackage? saved = null;
        await _packages.AddAsync(Arg.Do<SessionPackage>(p => saved = p), Arg.Any<CancellationToken>());
        var command = Package(7);

        var result = await _sut.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        saved!.TrainerId.Should().Be(command.TrainerId);
        saved.TotalSessions.Should().Be(7);
        saved.RemainingSessions.Should().Be(7);
        saved.IsTrial.Should().BeFalse();
        saved.Name.Should().Be("Free: 7 sessions");
        await _payments.DidNotReceiveWithAnyArgs().AddAsync(default!, default);
    }

    [Fact]
    public async Task Paid_package_is_named_custom_and_records_the_payment()
    {
        SessionPackage? saved = null;
        await _packages.AddAsync(Arg.Do<SessionPackage>(p => saved = p), Arg.Any<CancellationToken>());

        var result = await _sut.Handle(Package(6, 1200m, "Cash"), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        saved!.Name.Should().Be("Custom: 6 sessions");
        await _payments.ReceivedWithAnyArgs(1).AddAsync(default!, default);
    }
}
