namespace GymApp.Modules.Gyms.Application.Abstractions;

/// <summary>An uploaded import file to store under its gym and import job.</summary>
public sealed record ImportUpload(Guid GymId, Guid JobId, string OriginalFileName, Stream Content);

public interface IImportFileStorage
{
    Task<string> SaveUploadAsync(ImportUpload upload, CancellationToken cancellationToken = default);
    Task<Stream> OpenAsync(string storagePath, CancellationToken cancellationToken = default);
    Task<string> SaveErrorReportAsync(Guid jobId, Stream content, CancellationToken cancellationToken = default);
    Task<Stream> OpenErrorReportAsync(string storagePath, CancellationToken cancellationToken = default);
}
