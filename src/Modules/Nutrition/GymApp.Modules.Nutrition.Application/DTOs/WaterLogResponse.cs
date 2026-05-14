namespace GymApp.Modules.Nutrition.Application.DTOs;

public sealed record WaterLogResponse(
    Guid Id,
    Guid UserId,
    DateOnly Date,
    int TotalMl,
    int TargetMl,
    List<WaterEntryDto> Entries);
