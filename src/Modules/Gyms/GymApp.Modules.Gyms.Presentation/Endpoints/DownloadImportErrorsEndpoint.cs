using GymApp.Common.Presentation;
using GymApp.Modules.Gyms.Application.Queries.GetDataImportErrors;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace GymApp.Modules.Gyms.Presentation.Endpoints;

internal sealed class DownloadImportErrorsEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet("api/v1/gyms/{gymId:guid}/imports/{jobId:guid}/errors.csv",
            async (Guid gymId, Guid jobId, ISender sender, CancellationToken ct) =>
        {
            var result = await sender.Send(new GetDataImportErrorsQuery(gymId, jobId), ct);
            return result.IsSuccess
                ? Results.File(result.Value, "text/csv", $"import-{jobId:N}-errors.csv")
                : ApiResults.Problem(result.Error);
        })
        .WithTags("Gyms")
        .WithSummary("Download the CSV error report for an import job")
        .RequireAuthorization();
    }
}
