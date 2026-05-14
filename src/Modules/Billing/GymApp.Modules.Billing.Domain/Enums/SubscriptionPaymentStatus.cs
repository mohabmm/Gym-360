namespace GymApp.Modules.Billing.Domain.Enums;

public enum SubscriptionPaymentStatus
{
    Paid = 0,
    Overdue = 1,
    GracePeriod = 2,
    Suspended = 3
}
