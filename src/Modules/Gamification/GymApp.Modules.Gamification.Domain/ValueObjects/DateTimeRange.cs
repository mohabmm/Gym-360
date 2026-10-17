namespace GymApp.Modules.Gamification.Domain.ValueObjects;

/// <summary>An inclusive [From, To] timestamp window used to filter queries.</summary>
public sealed record DateTimeRange(DateTime From, DateTime To);
