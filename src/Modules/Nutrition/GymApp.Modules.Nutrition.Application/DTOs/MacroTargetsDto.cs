namespace GymApp.Modules.Nutrition.Application.DTOs;

public sealed record MacroTargetsDto(
    decimal ProteinGrams,
    decimal CarbsGrams,
    decimal FatGrams);
