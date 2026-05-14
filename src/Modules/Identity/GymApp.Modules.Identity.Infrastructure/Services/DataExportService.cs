using GymApp.Common.Application.Abstractions;
using GymApp.Modules.Identity.Application.Contracts;
using GymApp.Modules.Identity.Application.DTOs;
using Microsoft.Extensions.Logging;

namespace GymApp.Modules.Identity.Infrastructure.Services;

internal sealed class DataExportService : IDataExportService
{
    private readonly IEnumerable<IUserDataProvider> _dataProviders;
    private readonly ILogger<DataExportService> _logger;

    public DataExportService(
        IEnumerable<IUserDataProvider> dataProviders,
        ILogger<DataExportService> logger)
    {
        _dataProviders = dataProviders;
        _logger = logger;
    }

    public async Task<DataExportResponse> ExportUserDataAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Data export requested for user {UserId}", userId);

        var moduleData = new Dictionary<string, object>();

        foreach (var provider in _dataProviders)
        {
            try
            {
                var data = await provider.GetUserDataAsync(userId, cancellationToken);
                moduleData[provider.ModuleName] = data;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to retrieve data from module {Module} for user {UserId}",
                    provider.ModuleName, userId);
                moduleData[provider.ModuleName] = new { Error = "Data temporarily unavailable" };
            }
        }

        // In a real implementation this would serialize moduleData to JSON, upload as a ZIP to blob storage,
        // and return a signed download URL. For now we return a placeholder URL with the aggregated data logged.
        var expiresAt = DateTime.UtcNow.AddHours(24);
        var downloadUrl = $"https://storage.gymapp.eg/exports/{userId}/{Guid.NewGuid()}.zip";

        _logger.LogInformation("Data export prepared for user {UserId} with modules: {Modules}",
            userId, string.Join(", ", moduleData.Keys));

        return new DataExportResponse(downloadUrl, expiresAt);
    }
}
