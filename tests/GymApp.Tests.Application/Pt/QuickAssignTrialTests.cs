using GymApp.Modules.Billing.Domain.ValueObjects;
using GymApp.Common.Application.FeatureFlags;
using GymApp.Modules.Billing.Application.Abstractions;
using GymApp.Modules.Billing.Application.Commands.SessionPackages;
using GymApp.Modules.Billing.Domain.Entities;
using GymApp.Modules.Billing.Domain.Errors;
using GymApp.Modules.Billing.Domain.Repositories;

namespace GymApp.Tests.Application.Pt;

public class QuickAssignTrialTests
{
    private readonly ISessionPackageRepository _packages = Substitute.For<ISessionPackageRepository>();
    private readonly IPaymentRepository _payments = Substitute.For<IPaymentRepository>();
    private readonly IFeatureFlagService _flags = Substitute.For<IFeatureFlagService>();
    private readonly IBillingUnitOfWork _uow = Substitute.For<IBillingUnitOfWork>();
    private readonly QuickAssignSessionPackageCommandHandler _sut;

    private static readonly Guid GymId = Guid.NewGuid();

    public QuickAssignTrialTests()
    {
        _flags.IsEnabledAsync(GymId, Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(ci => ci.ArgAt<string>(1) != FeatureKeys.PtAutoExpiry);
        _sut = new QuickAssignSessionPackageCommandHandler(_packages, _payments, _flags, _uow);
    }

    private static QuickAssignSessionPackageCommand Trial(int sessions = 2, DateTime? expires = null) =>
        new(GymId, Guid.NewGuid(), Guid.NewGuid(), sessions, Guid.NewGuid(), ExpiresAt: expires, IsTrial: true);

    [Fact]
    public async Task Creates_a_free_trial_package_with_the_chosen_coach()
    {
        SessionPackage? saved = null;
        await _packages.AddAsync(Arg.Do<SessionPackage>(p => saved = p), Arg.Any<CancellationToken>());
        var command = Trial();

        var result = await _sut.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        saved!.IsTrial.Should().BeTrue();
        saved.TrainerId.Should().Be(command.TrainerId);
        saved.TotalSessions.Should().Be(2);
        saved.TotalPrice.Amount.Should().Be(0m);
        await _payments.DidNotReceiveWithAnyArgs().AddAsync(default!, default);
    }

    [Fact]
    public async Task Second_trial_for_the_same_member_is_rejected()
    {
        _packages.HasTrialAsync(Arg.Is<PackageOwner>(o => o.GymId == GymId), Arg.Any<CancellationToken>()).Returns(true);

        var result = await _sut.Handle(Trial(), CancellationToken.None);

        result.Error.Should().Be(BillingErrors.TrialAlreadyUsed);
    }

    [Fact]
    public async Task Trial_is_blocked_when_trial_sessions_flag_is_off()
    {
        _flags.IsEnabledAsync(GymId, FeatureKeys.PtTrialSessions, Arg.Any<CancellationToken>()).Returns(false);

        var result = await _sut.Handle(Trial(), CancellationToken.None);

        result.Error.Code.Should().Be("FEATURE_DISABLED:pt.trial_sessions");
    }

    [Fact]
    public async Task No_expiry_date_is_stored_while_auto_expiry_is_off()
    {
        SessionPackage? saved = null;
        await _packages.AddAsync(Arg.Do<SessionPackage>(p => saved = p), Arg.Any<CancellationToken>());

        await _sut.Handle(Trial(expires: DateTime.UtcNow.AddDays(30)), CancellationToken.None);

        saved!.ExpiresAt.Should().BeNull();
    }

    [Theory]
    [InlineData(0, false)]
    [InlineData(1, true)]
    [InlineData(3, true)]
    [InlineData(4, false)]
    public void Validator_allows_one_to_three_free_trial_sessions(int sessions, bool valid)
    {
        var validator = new QuickAssignSessionPackageCommandValidator();

        validator.Validate(Trial(sessions)).IsValid.Should().Be(valid);
    }

    [Fact]
    public void Validator_rejects_a_priced_trial()
    {
        var validator = new QuickAssignSessionPackageCommandValidator();
        var priced = Trial() with { TotalPrice = 500m };

        validator.Validate(priced).IsValid.Should().BeFalse();
    }
}
