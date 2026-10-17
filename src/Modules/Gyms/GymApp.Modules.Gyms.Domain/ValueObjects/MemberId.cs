namespace GymApp.Modules.Gyms.Domain.ValueObjects;

/// <summary>Identifies a user on a gym's member roster (an Identity user id).</summary>
public readonly record struct MemberId(Guid Value);
