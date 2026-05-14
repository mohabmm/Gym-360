namespace GymApp.Modules.Scheduling.Infrastructure.Persistence.ReadModels;

/// <summary>
/// Lightweight read model for the gyms.gym_session_policies table.
/// This avoids cross-module Domain/Infrastructure references while
/// allowing the Scheduling module to read gym cancellation policies
/// from the shared SQL Server database.
/// </summary>
internal sealed class GymSessionPolicyReadModel
{
    public Guid Id { get; set; }
    public Guid GymId { get; set; }
    public int CancellationWindowHours { get; set; }
    public int NoshowThreshold { get; set; }
    public int NoshowWindowDays { get; set; }
    public string NoshowAction { get; set; } = default!;
}
