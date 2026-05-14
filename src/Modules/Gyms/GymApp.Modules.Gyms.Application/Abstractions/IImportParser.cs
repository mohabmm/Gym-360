using GymApp.Modules.Gyms.Domain.Enums;
using GymApp.Modules.Gyms.IntegrationEvents;

namespace GymApp.Modules.Gyms.Application.Abstractions;

public interface IImportParser
{
    bool CanHandle(DataImportSource source);

    Task<ParsedImport> ParseAsync(Guid importJobId, Guid gymId, Stream content, CancellationToken cancellationToken = default);
}

public sealed record ParsedImport(
    IReadOnlyList<DataImportMemberIntegrationEvent> Members,
    IReadOnlyList<DataImportPaymentIntegrationEvent> Payments,
    IReadOnlyList<DataImportSessionPackageIntegrationEvent> Packages,
    IReadOnlyList<DataImportInBodyIntegrationEvent> InBodyRecords,
    IReadOnlyList<ImportRowError> Errors);

public sealed record ImportRowError(string Sheet, int RowNumber, string Column, string Message);
