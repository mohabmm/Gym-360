namespace GymApp.Modules.Nutrition.Domain.ValueObjects;

public sealed record MacroTargets(
    decimal ProteinGrams,
    decimal CarbsGrams,
    decimal FatGrams);
