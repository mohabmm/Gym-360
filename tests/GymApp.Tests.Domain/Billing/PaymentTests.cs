using GymApp.Common.Domain;
using GymApp.Modules.Billing.Domain.Entities;
using GymApp.Modules.Billing.Domain.Enums;
using GymApp.Modules.Billing.Domain.Events;
using GymApp.Modules.Billing.Domain.ValueObjects;

namespace GymApp.Tests.Domain.Billing;

public sealed class PaymentTests
{
    private static Payment CreateDefaultPayment()
    {
        return Payment.Create(
            Guid.NewGuid(),
            Guid.NewGuid(),
            Money.Create(500m, "EGP"),
            PaymentMethod.Cash,
            PaymentPurpose.Membership,
            "REC-001",
            Guid.NewGuid());
    }

    [Fact]
    public void Create_ShouldSetPropertiesCorrectly()
    {
        var gymId = Guid.NewGuid();
        var memberId = Guid.NewGuid();
        var recordedBy = Guid.NewGuid();

        var payment = Payment.Create(
            gymId, memberId,
            Money.Create(1000m, "EGP"),
            PaymentMethod.Card,
            PaymentPurpose.SessionPackage,
            "REC-100",
            recordedBy);

        payment.Id.Should().NotBeEmpty();
        payment.GymId.Should().Be(gymId);
        payment.MemberId.Should().Be(memberId);
        payment.Amount.Amount.Should().Be(1000m);
        payment.Amount.Currency.Should().Be("EGP");
        payment.PaymentMethod.Should().Be(PaymentMethod.Card);
        payment.Purpose.Should().Be(PaymentPurpose.SessionPackage);
        payment.Status.Should().Be(PaymentStatus.Completed);
        payment.ReceiptNumber.Should().Be("REC-100");
        payment.PaidAt.Should().NotBeNull();
    }

    [Fact]
    public void Create_ShouldRaisePaymentReceivedDomainEvent()
    {
        var payment = CreateDefaultPayment();

        payment.GetDomainEvents().Should().ContainSingle()
            .Which.Should().BeOfType<PaymentReceivedDomainEvent>();
    }

    // --- Void (no refund per Q9) ---

    [Fact]
    public void Void_WhenCompleted_ShouldSetVoidedStatusAndRaiseEvent()
    {
        var payment = CreateDefaultPayment();
        payment.ClearDomainEvents();

        var result = payment.Void("Customer requested");

        result.IsSuccess.Should().BeTrue();
        payment.Status.Should().Be(PaymentStatus.Voided);
        payment.VoidReason.Should().Be("Customer requested");
        payment.GetDomainEvents().Should().ContainSingle()
            .Which.Should().BeOfType<PaymentVoidedDomainEvent>();
    }

    [Fact]
    public void Void_WhenAlreadyVoided_ShouldFail()
    {
        var payment = CreateDefaultPayment();
        payment.Void("First void");

        var result = payment.Void("Second void");

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Billing.PaymentAlreadyVoided");
    }
}

public sealed class SessionPackageTests
{
    private static SessionPackage CreateDefaultPackage(int totalSessions = 10, DateTime? expiresAt = null)
    {
        return SessionPackage.Create(
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            "PT Package - 10 Sessions",
            totalSessions,
            Money.Create(200m, "EGP"),
            Money.Create(2000m, "EGP"),
            Guid.NewGuid(),
            expiresAt);
    }

    [Fact]
    public void Create_ShouldSetPropertiesCorrectly()
    {
        var package = CreateDefaultPackage(10);

        package.Id.Should().NotBeEmpty();
        package.TotalSessions.Should().Be(10);
        package.RemainingSessions.Should().Be(10);
        package.Status.Should().Be(PackageStatus.Active);
        package.Name.Should().Be("PT Package - 10 Sessions");
        package.PricePerSession.Amount.Should().Be(200m);
        package.TotalPrice.Amount.Should().Be(2000m);
    }

    [Fact]
    public void UseSession_ShouldDecrementRemainingSessions()
    {
        var package = CreateDefaultPackage(10);

        var result = package.UseSession(Guid.NewGuid());

        result.IsSuccess.Should().BeTrue();
        package.RemainingSessions.Should().Be(9);
        package.UsageHistory.Should().ContainSingle();
        package.UsageHistory[0].UsageType.Should().Be(PackageUsageType.Completed);
    }

    [Fact]
    public void UseSession_WhenLowSessions_ShouldRaiseLowEvent()
    {
        var package = CreateDefaultPackage(4);
        package.UseSession(Guid.NewGuid()); // 3 remaining
        package.ClearDomainEvents();

        // At exactly 3 remaining, the first usage should have raised the event
        // Let's verify by checking remaining and creating a fresh scenario
        var pkg = CreateDefaultPackage(4);

        pkg.UseSession(Guid.NewGuid()); // 3 remaining

        pkg.GetDomainEvents().Should().ContainSingle()
            .Which.Should().BeOfType<SessionPackageLowDomainEvent>();
    }

    [Fact]
    public void UseSession_WhenExhausted_ShouldSetExhaustedAndRaiseEvent()
    {
        var package = CreateDefaultPackage(1);

        package.UseSession(Guid.NewGuid());

        package.RemainingSessions.Should().Be(0);
        package.Status.Should().Be(PackageStatus.Exhausted);
        package.GetDomainEvents().Should().ContainSingle()
            .Which.Should().BeOfType<SessionPackageExhaustedDomainEvent>();
    }

    [Fact]
    public void UseSession_WhenAlreadyExhausted_ShouldFail()
    {
        var package = CreateDefaultPackage(1);
        package.UseSession(Guid.NewGuid());

        var result = package.UseSession(Guid.NewGuid());

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Billing.PackageExhausted");
    }

    [Fact]
    public void UseSession_WhenExpired_ShouldFail()
    {
        var package = CreateDefaultPackage(10);
        package.Expire();

        var result = package.UseSession(Guid.NewGuid());

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Billing.PackageExpired");
    }

    // --- Late cancel deduction (per Q5/FOUNDER_DECISIONS) ---

    [Fact]
    public void RecordLateCancelUsage_ShouldDeductSessionAndRecordUsage()
    {
        var package = CreateDefaultPackage(10);

        var result = package.RecordLateCancelUsage(Guid.NewGuid());

        result.IsSuccess.Should().BeTrue();
        package.RemainingSessions.Should().Be(9);
        package.UsageHistory.Should().ContainSingle();
        package.UsageHistory[0].UsageType.Should().Be(PackageUsageType.LateCancelled);
    }

    [Fact]
    public void RecordLateCancelUsage_WhenExhausted_ShouldFail()
    {
        var package = CreateDefaultPackage(1);
        package.UseSession(Guid.NewGuid());

        var result = package.RecordLateCancelUsage(Guid.NewGuid());

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Billing.PackageExhausted");
    }

    [Fact]
    public void RecordLateCancelUsage_WhenExpired_ShouldFail()
    {
        var package = CreateDefaultPackage(10);
        package.Expire();

        var result = package.RecordLateCancelUsage(Guid.NewGuid());

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Billing.PackageExpired");
    }

    [Fact]
    public void RecordLateCancelUsage_LastSession_ShouldExhaust()
    {
        var package = CreateDefaultPackage(1);

        package.RecordLateCancelUsage(Guid.NewGuid());

        package.RemainingSessions.Should().Be(0);
        package.Status.Should().Be(PackageStatus.Exhausted);
    }

    [Fact]
    public void Expire_ShouldSetExpiredStatus()
    {
        var package = CreateDefaultPackage(10);

        package.Expire();

        package.Status.Should().Be(PackageStatus.Expired);
    }

    [Fact]
    public void UseSession_MultipleTimes_ShouldTrackAllUsage()
    {
        var package = CreateDefaultPackage(5);

        for (int i = 0; i < 3; i++)
            package.UseSession(Guid.NewGuid());

        package.RemainingSessions.Should().Be(2);
        package.UsageHistory.Should().HaveCount(3);
    }
}

public sealed class MoneyTests
{
    [Fact]
    public void Create_ValidAmount_ShouldSucceed()
    {
        var money = Money.Create(100m, "EGP");

        money.Amount.Should().Be(100m);
        money.Currency.Should().Be("EGP");
    }

    [Fact]
    public void Create_ZeroAmount_ShouldSucceed()
    {
        var money = Money.Create(0m);

        money.Amount.Should().Be(0m);
    }

    [Fact]
    public void Create_NegativeAmount_ShouldThrow()
    {
        var act = () => Money.Create(-1m);

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Create_DefaultCurrency_ShouldBeEGP()
    {
        var money = Money.Create(100m);

        money.Currency.Should().Be("EGP");
    }

    [Fact]
    public void Zero_ShouldReturnZeroAmountInEGP()
    {
        var zero = Money.Zero();

        zero.Amount.Should().Be(0m);
        zero.Currency.Should().Be("EGP");
    }

    [Fact]
    public void Create_EmptyCurrency_ShouldThrow()
    {
        var act = () => Money.Create(100m, "");

        act.Should().Throw<ArgumentException>();
    }
}
