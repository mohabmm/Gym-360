using GymApp.Modules.Identity.Application.DTOs;

namespace GymApp.Modules.Identity.Application.Contracts;

public interface IDataExportService
{
    Task<DataExportResponse> ExportUserDataAsync(Guid userId, CancellationToken cancellationToken = default);
}
