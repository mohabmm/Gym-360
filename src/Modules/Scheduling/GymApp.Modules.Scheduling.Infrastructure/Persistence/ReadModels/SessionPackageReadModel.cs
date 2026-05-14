namespace GymApp.Modules.Scheduling.Infrastructure.Persistence.ReadModels;

/// <summary>
/// Lightweight read model for billing.session_packages.
/// Allows the Scheduling module to validate package expiry at booking time
/// without referencing Billing.Domain or Billing.Infrastructure directly.
/// </summary>
internal sealed class SessionPackageReadModel
{
    public Guid Id { get; set; }
    public Guid GymId { get; set; }
    public Guid MemberId { get; set; }
    public string Status { get; set; } = default!;
    public int TotalSessions { get; set; }
    public int RemainingSessions { get; set; }
    public DateTime? ExpiresAt { get; set; }
}
