namespace GymApp.Modules.Gyms.Infrastructure.Jobs;

/// <summary>
/// Lightweight read model for billing.gym_subscriptions.
/// Avoids cross-module project references while allowing the Gyms module
/// to read subscription payment state from the shared SQL Server database.
/// </summary>
internal sealed class GymSubscriptionReadModel
{
    public Guid Id { get; set; }
    public Guid GymId { get; set; }
    public string PaymentStatus { get; set; } = default!;
    public DateTime? NextPaymentDue { get; set; }
    public DateTime? OverdueSince { get; set; }
    public DateTime? GracePeriodEndsAt { get; set; }
}
