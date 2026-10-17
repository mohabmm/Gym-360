namespace GymApp.Modules.Progress.Domain.ValueObjects;

/// <summary>An inclusive calendar date range.</summary>
public sealed record DateRange(DateOnly From, DateOnly To);
