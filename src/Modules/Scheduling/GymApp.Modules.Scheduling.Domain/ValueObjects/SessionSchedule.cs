namespace GymApp.Modules.Scheduling.Domain.ValueObjects;

/// <summary>Gym-local wall-clock date and time a session is booked for, plus its length.</summary>
public sealed record SessionSchedule(DateOnly Date, TimeOnly Time, int DurationMinutes);
