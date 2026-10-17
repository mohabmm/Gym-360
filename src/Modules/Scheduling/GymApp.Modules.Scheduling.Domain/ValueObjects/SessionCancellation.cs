namespace GymApp.Modules.Scheduling.Domain.ValueObjects;

/// <summary>Why and by whom a session is cancelled, and whether it falls inside the late-cancel window.</summary>
public sealed record SessionCancellation(string Reason, Guid CancelledBy, bool IsLateCancellation);
