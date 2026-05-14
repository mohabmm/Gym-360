namespace GymApp.Modules.Nutrition.Application.DTOs;

public sealed record WaterEntryDto(
    Guid Id,
    int AmountMl,
    DateTime LoggedAt);
