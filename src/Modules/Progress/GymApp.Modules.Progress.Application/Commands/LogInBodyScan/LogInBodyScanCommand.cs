using GymApp.Common.Application.Messaging;

namespace GymApp.Modules.Progress.Application.Commands.LogInBodyScan;

/// <summary>Values the member confirmed after scanning an InBody result sheet. All masses in kg.</summary>
public sealed record InBodyScanValues(
    decimal? WeightKg,
    decimal? HeightCm,
    decimal? BodyFatPercentage,
    decimal? SkeletalMuscleMassKg,
    decimal? BodyWaterKg);

public sealed record LogInBodyScanCommand(
    Guid UserId,
    Guid GymId,
    DateOnly Date,
    InBodyScanValues Values,
    string? Notes) : ICommand<LogInBodyScanResult>;

/// <param name="EntryId">The body-metric entry the scan was saved to.</param>
/// <param name="Created">False when the scan updated an entry already logged for that day.</param>
public sealed record LogInBodyScanResult(Guid EntryId, bool Created);
