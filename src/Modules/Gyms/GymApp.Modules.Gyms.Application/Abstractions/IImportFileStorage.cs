namespace GymApp.Modules.Gyms.Application.Abstractions;

public interface IImportFileStorage
{
    Task<string> SaveUploadAsync(Guid gymId, Guid jobId, string originalFileName, Stream content, CancellationToken cancellationToken = default);
    Task<Stream> OpenAsync(string storagePath, CancellationToken cancellationToken = default);
    Task<string> SaveErrorReportAsync(Guid jobId, Stream content, CancellationToken cancellationToken = default);
    Task<Stream> OpenErrorReportAsync(string storagePath, CancellationToken cancellationToken = default);
}
