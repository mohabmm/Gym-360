using GymApp.Modules.Billing.Application.DTOs;

namespace GymApp.Modules.Billing.Application.Abstractions;

// Infrastructure-backed reader that aggregates across Billing DbContext and cross-module read
// models. Kept as an application-level contract so the handler stays free of DbContext deps
// and we can swap the implementation (e.g. a materialised read-model table) later.
public interface IPlatformAnalyticsReader
{
    Task<PlatformAnalyticsDto> GetPlatformAnalyticsAsync(CancellationToken cancellationToken);
    Task<ChurnAnalyticsDto> GetChurnAnalyticsAsync(CancellationToken cancellationToken);
}
