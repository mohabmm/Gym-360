using FluentAssertions;
using GymApp.Modules.Billing.Domain.Entities;
using GymApp.Modules.Billing.Domain.Enums;
using GymApp.Modules.Billing.Domain.ValueObjects;

namespace GymApp.Tests.Domain.Billing;

/// <summary>
/// Guards that session usage is idempotent. Once integration events are delivered via the
/// at-least-once outbox, SessionCompleted/SessionLateCancelled handlers may re-run for the same
/// session; the package must never be double-deducted.
/// </summary>
public sealed class SessionPackageIdempotencyTests
{
    private static SessionPackage CreateActivePackage(int totalSessions = 10) =>
        SessionPackage.Create(new SessionPackageDetails
        {
            GymId = Guid.NewGuid(),
            TrainerId = Guid.NewGuid(),
            MemberId = Guid.NewGuid(),
            TemplateId = Guid.NewGuid(),
            Name = "10-pack",
            TotalSessions = totalSessions,
            PricePerSession = Money.Create(100m, "EGP"),
            TotalPrice = Money.Create(1000m, "EGP"),
            AssignedBy = Guid.NewGuid()
        });

    [Fact]
    public void UseSession_RepeatedForSameSession_DeductsOnce()
    {
        var package = CreateActivePackage();
        var sessionId = Guid.NewGuid();

        var first = package.UseSession(sessionId);
        var second = package.UseSession(sessionId);

        first.IsSuccess.Should().BeTrue();
        second.IsSuccess.Should().BeTrue(); // no-op success, not a failure
        package.RemainingSessions.Should().Be(9);
        package.UsageHistory.Count(u => u.SessionId == sessionId && u.UsageType == PackageUsageType.Completed)
            .Should().Be(1);
    }

    [Fact]
    public void UseSession_DistinctSessions_DeductsEach()
    {
        var package = CreateActivePackage();

        package.UseSession(Guid.NewGuid());
        package.UseSession(Guid.NewGuid());

        package.RemainingSessions.Should().Be(8);
    }

    [Fact]
    public void RecordLateCancelUsage_RepeatedForSameSession_DeductsOnce()
    {
        var package = CreateActivePackage();
        var sessionId = Guid.NewGuid();

        var first = package.RecordLateCancelUsage(sessionId);
        var second = package.RecordLateCancelUsage(sessionId);

        first.IsSuccess.Should().BeTrue();
        second.IsSuccess.Should().BeTrue();
        package.RemainingSessions.Should().Be(9);
        package.UsageHistory.Count(u => u.SessionId == sessionId && u.UsageType == PackageUsageType.LateCancelled)
            .Should().Be(1);
    }

    [Fact]
    public void UseSession_ThenLateCancel_SameSessionId_AreTrackedIndependently()
    {
        // Completed and LateCancelled are distinct usage types; a real session is only ever one,
        // but the guard keys on (sessionId, type) so the two paths never mask each other.
        var package = CreateActivePackage();
        var sessionId = Guid.NewGuid();

        package.UseSession(sessionId);
        package.RecordLateCancelUsage(sessionId);

        package.RemainingSessions.Should().Be(8);
    }
}
