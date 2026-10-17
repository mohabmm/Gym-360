namespace GymApp.Modules.Scheduling.Domain.ValueObjects;

/// <summary>How early before its scheduled time a session may be started.</summary>
/// <param name="EarlyWindowMinutes">Minutes before the scheduled start that starting is allowed.</param>
/// <param name="AllowEarlyStart">When true, the early window is not enforced at all.</param>
public sealed record EarlyStartPolicy(int EarlyWindowMinutes, bool AllowEarlyStart);
