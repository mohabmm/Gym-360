namespace GymApp.Modules.Gyms.Domain.ValueObjects;

/// <summary>Identifies a gym (the Gym aggregate's id) in Gyms-module APIs, so a gym id cannot be passed where a member or trainer id is expected.</summary>
public readonly record struct GymId(Guid Value);
