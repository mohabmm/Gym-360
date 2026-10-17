namespace GymApp.Modules.Nutrition.Domain.ValueObjects;

/// <summary>An amount of water a user drank on a given day, within a gym tenant.</summary>
public sealed record WaterIntake(Guid UserId, Guid GymId, DateOnly Date, int AmountMl);
