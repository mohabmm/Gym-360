using GymApp.Common.Application.FeatureFlags;
using GymApp.Modules.Billing.Application.Abstractions;
using GymApp.Modules.Billing.Application.Commands.SessionPackages;
using GymApp.Modules.Billing.Domain.Entities;
using GymApp.Modules.Billing.Domain.Errors;
using GymApp.Modules.Billing.Domain.Repositories;
using GymApp.Modules.Billing.Domain.ValueObjects;

namespace GymApp.Tests.Application.Billing;

/// <summary>Staff "Assign Package" (template): one coach at a time, renewals with the same coach allowed.</summary>
public class AssignSessionPackageCommandHandlerTests
{
    private readonly ISessionPackageRepository _packages = Substitute.For<ISessionPackageRepository>();
    private readonly ISessionPackageTemplateRepository _templates = Substitute.For<ISessionPackageTemplateRepository>();
    private readonly IFeatureFlagService _flags = Substitute.For<IFeatureFlagService>();
    private readonly IBillingUnitOfWork _uow = Substitute.For<IBillingUnitOfWork>();
    private readonly AssignSessionPackageCommandHandler _sut;

    private static readonly Guid GymId = Guid.NewGuid();
    private static readonly Guid MemberId = Guid.NewGuid();
    private static readonly Guid CoachId = Guid.NewGuid();

    private readonly SessionPackageTemplate _template = SessionPackageTemplate.Create(new SessionPackageTemplateDetails
    {
        GymId = GymId,
        Name = "12 sessions",
        SessionCount = 12,
        PricePerSession = Money.Create(150),
        TotalPrice = Money.Create(1800),
    });

    public AssignSessionPackageCommandHandlerTests()
    {
        _flags.IsEnabledAsync(GymId, Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(ci => ci.ArgAt<string>(1) != FeatureKeys.PtAutoExpiry);
        _templates.GetByIdAsync(_template.Id, Arg.Any<CancellationToken>()).Returns(_template);
        _sut = new AssignSessionPackageCommandHandler(_packages, _templates, _flags, _uow);
    }

    private AssignSessionPackageCommand Assign(Guid trainerId, Guid? assignedBy = null) =>
        new(GymId, trainerId, MemberId, _template.Id, assignedBy ?? Guid.NewGuid(), ExpiresAt: null);

    private void MemberHasSessionsLeftWith(Guid trainerId)
    {
        _packages.HasActivePackageAsync(Arg.Is<PackageOwner>(o => o.MemberId == MemberId), Arg.Any<DateTime>(), Arg.Any<CancellationToken>())
            .Returns(true);
        var existing = SessionPackage.Create(new SessionPackageDetails
        {
            GymId = GymId,
            TrainerId = trainerId,
            MemberId = MemberId,
            TemplateId = _template.Id,
            Name = "Old",
            TotalSessions = 8,
            PricePerSession = Money.Create(150),
            TotalPrice = Money.Create(1200),
            AssignedBy = Guid.NewGuid(),
        });
        _packages.GetActiveByMemberAndTrainerAsync(
                Arg.Is<PackageParticipants>(p => p.MemberId == MemberId && p.TrainerId == trainerId), Arg.Any<CancellationToken>())
            .Returns(existing);
    }

    [Fact]
    public async Task New_member_gets_the_template_package_recorded_as_assigned_by_the_staff_member()
    {
        SessionPackage? saved = null;
        await _packages.AddAsync(Arg.Do<SessionPackage>(p => saved = p), Arg.Any<CancellationToken>());
        var staffId = Guid.NewGuid();

        var result = await _sut.Handle(Assign(CoachId, staffId), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        saved!.TrainerId.Should().Be(CoachId);
        saved.TotalSessions.Should().Be(12);
        saved.AssignedBy.Should().Be(staffId);
    }

    [Fact]
    public async Task Renewal_with_the_same_coach_while_sessions_are_left_is_allowed()
    {
        MemberHasSessionsLeftWith(CoachId);

        var result = await _sut.Handle(Assign(CoachId), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        await _packages.ReceivedWithAnyArgs(1).AddAsync(default!, default);
    }

    [Fact]
    public async Task A_different_coach_while_sessions_are_left_is_rejected()
    {
        MemberHasSessionsLeftWith(CoachId);

        var result = await _sut.Handle(Assign(Guid.NewGuid()), CancellationToken.None);

        result.Error.Should().Be(BillingErrors.CoachAlreadyAssigned);
        await _packages.DidNotReceiveWithAnyArgs().AddAsync(default!, default);
    }

    [Fact]
    public async Task Unknown_template_is_rejected()
    {
        var result = await _sut.Handle(
            new AssignSessionPackageCommand(GymId, CoachId, MemberId, Guid.NewGuid(), Guid.NewGuid(), null),
            CancellationToken.None);

        result.Error.Should().Be(BillingErrors.TemplateNotFound);
    }
}
