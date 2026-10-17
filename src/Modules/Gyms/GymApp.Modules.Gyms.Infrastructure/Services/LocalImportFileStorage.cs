using GymApp.Modules.Gyms.Application.Abstractions;
using Microsoft.Extensions.Configuration;

namespace GymApp.Modules.Gyms.Infrastructure.Services;

internal sealed class LocalImportFileStorage : IImportFileStorage
{
    private readonly string _rootPath;

    public LocalImportFileStorage(IConfiguration configuration)
    {
        _rootPath = configuration["DataImport:StorageRoot"]
            ?? Path.Combine(Path.GetTempPath(), "gymapp-imports");
        Directory.CreateDirectory(_rootPath);
    }

    public async Task<string> SaveUploadAsync(ImportUpload upload, CancellationToken cancellationToken = default)
    {
        var (gymId, jobId, originalFileName, content) = upload;
        var gymDir = Path.Combine(_rootPath, gymId.ToString("N"), "uploads");
        Directory.CreateDirectory(gymDir);

        var safeName = Path.GetFileName(originalFileName);
        var relative = Path.Combine(gymId.ToString("N"), "uploads", $"{jobId:N}_{safeName}");
        var absolute = Path.Combine(_rootPath, relative);

        await using var fs = File.Create(absolute);
        await content.CopyToAsync(fs, cancellationToken);

        return relative;
    }

    public Task<Stream> OpenAsync(string storagePath, CancellationToken cancellationToken = default)
    {
        var absolute = Path.Combine(_rootPath, storagePath);
        Stream stream = File.OpenRead(absolute);
        return Task.FromResult(stream);
    }

    public async Task<string> SaveErrorReportAsync(Guid jobId, Stream content, CancellationToken cancellationToken = default)
    {
        var dir = Path.Combine(_rootPath, "errors");
        Directory.CreateDirectory(dir);

        var relative = Path.Combine("errors", $"{jobId:N}.csv");
        var absolute = Path.Combine(_rootPath, relative);

        await using var fs = File.Create(absolute);
        await content.CopyToAsync(fs, cancellationToken);

        return relative;
    }

    public Task<Stream> OpenErrorReportAsync(string storagePath, CancellationToken cancellationToken = default) =>
        OpenAsync(storagePath, cancellationToken);
}
