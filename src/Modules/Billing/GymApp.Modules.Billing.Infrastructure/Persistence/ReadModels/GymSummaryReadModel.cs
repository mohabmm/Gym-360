namespace GymApp.Modules.Billing.Infrastructure.Persistence.ReadModels;

/// <summary>
/// Lightweight read model for gyms.gyms.
/// Provides admin analytics with gym name, status, city, and timestamps — without taking a
/// dependency on Gyms.Domain or Gyms.Infrastructure.
/// </summary>
internal sealed class GymSummaryReadModel
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string StatusText { get; set; } = string.Empty;
    public string? City { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    // Not mapped — filled in by a separate projection when needed.
    public int MemberCount { get; set; }
    public int TrainerCount { get; set; }
}
