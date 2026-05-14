using GymApp.Common.Presentation;
using GymApp.Modules.Gyms.Application.Queries.GetDataImportJob;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace GymApp.Modules.Gyms.Presentation.Endpoints;

internal sealed class GetDataImportJobEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet("api/v1/gyms/{gymId:guid}/imports/{jobId:guid}",
            async (Guid gymId, Guid jobId, ISender sender, CancellationToken ct) =>
        {
            var result = await sender.Send(new GetDataImportJobQuery(gymId, jobId), ct);
            return result.IsSuccess ? ApiResults.Ok(result.Value) : ApiResults.Problem(result.Error);
        })
        .WithTags("Gyms")
        .WithSummary("Get data import job status")
        .RequireAuthorization();
    }
}
