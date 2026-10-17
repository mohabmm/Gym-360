using GymApp.Common.Domain;
using GymApp.Modules.Billing.Domain.Enums;
using GymApp.Modules.Billing.Domain.Events;
using GymApp.Modules.Billing.Domain.ValueObjects;

namespace GymApp.Modules.Billing.Domain.Entities;

public sealed class GymSubscription : Entity
{
    private GymSubscription() { }

    public Guid GymId { get; private set; }
    public Guid PlanId { get; private set; }
    public DateTime StartedAt { get; private set; }
    public DateTime? ExpiresAt { get; private set; }
    public bool IsActive { get; private set; }
    public bool IsTrial { get; private set; }
    public DateTime? TrialExpiresAt { get; private set; }
    public SubscriptionPaymentStatus PaymentStatus { get; private set; }
    public DateTime? LastPaymentAt { get; private set; }
    public DateTime? NextPaymentDue { get; private set; }
    public DateTime? OverdueSince { get; private set; }
    public DateTime? GracePeriodEndsAt { get; private set; }

    public static GymSubscription Create(GymSubscriptionDetails details)
    {
        return new GymSubscription
        {
            Id = Guid.NewGuid(),
            GymId = details.GymId,
            PlanId = details.PlanId,
            StartedAt = DateTime.UtcNow,
            ExpiresAt = details.ExpiresAt,
            IsActive = true,
            IsTrial = details.IsTrial,
            TrialExpiresAt = details.TrialExpiresAt,
            PaymentStatus = SubscriptionPaymentStatus.Paid
        };
    }

    public void ChangePlan(Guid newPlanId)
    {
        var oldPlanId = PlanId;
        PlanId = newPlanId;

        RaiseDomainEvent(new GymPlanChangedDomainEvent(GymId, oldPlanId, newPlanId));
    }

    public void RecordPayment()
    {
        PaymentStatus = SubscriptionPaymentStatus.Paid;
        LastPaymentAt = DateTime.UtcNow;
        OverdueSince = null;
        GracePeriodEndsAt = null;
    }

    public void MarkOverdue(DateTime? gracePeriodEndsAt = null)
    {
        PaymentStatus = SubscriptionPaymentStatus.Overdue;
        OverdueSince = DateTime.UtcNow;
        GracePeriodEndsAt = gracePeriodEndsAt;
    }

    public void EnterGracePeriod(DateTime gracePeriodEndsAt)
    {
        PaymentStatus = SubscriptionPaymentStatus.GracePeriod;
        GracePeriodEndsAt = gracePeriodEndsAt;
    }

    public void Suspend()
    {
        PaymentStatus = SubscriptionPaymentStatus.Suspended;
        IsActive = false;
    }

    public void Reactivate()
    {
        PaymentStatus = SubscriptionPaymentStatus.Paid;
        IsActive = true;
        OverdueSince = null;
        GracePeriodEndsAt = null;
    }
}
