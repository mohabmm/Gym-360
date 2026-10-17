using GymApp.Modules.Identity.Domain.Enums;

namespace GymApp.Modules.Identity.Domain.ValueObjects;

/// <summary>The commercial terms of a <see cref="Entities.Contract"/>: dashboard mode, plan tier and validity window.</summary>
public sealed record ContractDetails
{
    public required DashboardMode DashboardMode { get; init; }
    public required string PlanTier { get; init; }
    public required DateTime EffectiveFrom { get; init; }
    public DateTime? EffectiveTo { get; init; }
}
