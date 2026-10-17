using GymApp.Modules.Gyms.Domain.Enums;

namespace GymApp.Modules.Gyms.Domain.ValueObjects;

/// <summary>A gym's cancellation window and no-show enforcement rules.</summary>
public sealed record SessionPolicyRules
{
    public required int CancellationWindowHours { get; init; }
    public required int NoshowThreshold { get; init; }
    public required int NoshowWindowDays { get; init; }
    public required NoshowAction NoshowAction { get; init; }
}
