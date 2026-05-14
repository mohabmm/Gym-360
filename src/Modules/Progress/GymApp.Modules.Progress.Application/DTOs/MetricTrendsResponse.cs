namespace GymApp.Modules.Progress.Application.DTOs;

public sealed record MetricTrendsResponse(
    List<string> Labels,
    List<decimal?> WeightValues,
    List<decimal?> BodyFatValues,
    List<decimal?> WaistValues,
    List<decimal?> ChestValues);
