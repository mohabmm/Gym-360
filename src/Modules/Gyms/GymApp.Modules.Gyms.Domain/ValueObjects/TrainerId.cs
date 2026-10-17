namespace GymApp.Modules.Gyms.Domain.ValueObjects;

/// <summary>Identifies a user on a gym's trainer roster (an Identity user id).</summary>
public readonly record struct TrainerId(Guid Value);
