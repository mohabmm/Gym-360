namespace GymApp.Modules.Scheduling.Domain.ValueObjects;

/// <summary>An inclusive range of scheduled dates within one gym.</summary>
public sealed record GymDateRange(Guid GymId, DateOnly From, DateOnly To);
