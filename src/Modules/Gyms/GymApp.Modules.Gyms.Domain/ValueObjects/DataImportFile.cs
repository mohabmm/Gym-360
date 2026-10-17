using GymApp.Modules.Gyms.Domain.Enums;

namespace GymApp.Modules.Gyms.Domain.ValueObjects;

/// <summary>The uploaded file a data import job processes and where it is stored.</summary>
public sealed record DataImportFile
{
    public required DataImportSource Source { get; init; }
    public required string OriginalFileName { get; init; }
    public required string StoragePath { get; init; }
    public required long FileSizeBytes { get; init; }
}
