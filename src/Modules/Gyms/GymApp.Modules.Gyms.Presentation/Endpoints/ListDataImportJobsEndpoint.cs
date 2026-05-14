using GymApp.Common.Presentation;
using GymApp.Modules.Gyms.Application.Queries.ListDataImportJobs;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace GymApp.Modules.Gyms.Presentation.Endpoints;

internal sealed class ListDataImportJobsEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet("api/v1/gyms/{gymId:guid}/imports",
            async (Guid gymId, int? skip, int? take, ISender sender, CancellationToken ct) =>
        {
            var result = await sender.Send(
                new ListDataImportJobsQuery(gymId, skip ?? 0, take ?? 20), ct);
            return result.IsSuccess ? ApiResults.Ok(result.Value) : ApiResults.Problem(result.Error);
        })
        .WithTags("Gyms")
        .WithSummary("List data import jobs for a gym")
        .RequireAuthorization();
    }
}
