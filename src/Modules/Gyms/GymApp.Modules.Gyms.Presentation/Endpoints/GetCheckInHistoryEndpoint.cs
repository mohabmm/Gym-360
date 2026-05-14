using GymApp.Common.Presentation;
using GymApp.Modules.Gyms.Application.DTOs;
using GymApp.Modules.Gyms.Application.Queries.GetCheckInHistory;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace GymApp.Modules.Gyms.Presentation.Endpoints;

internal sealed class GetCheckInHistoryEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet("api/v1/gyms/{gymId:guid}/check-ins", async (
            Guid gymId,
            DateTime? from,
            DateTime? to,
            int page,
            int pageSize,
            ISender sender) =>
        {
            var query = new GetCheckInHistoryQuery(gymId, from, to, page, pageSize);

            var result = await sender.Send(query);

            return result.IsSuccess
                ? ApiResults.Ok(result.Value)
                : ApiResults.Problem(result.Error);
        })
        .WithTags("Gyms")
        .WithSummary("Get paginated check-in history")
        .WithDescription("Returns a paginated list of check-in records for the gym, optionally filtered by date range.")
        .Produces<ApiResponse<PagedResponse<CheckInDto>>>(200)
        .Produces<ApiResponse<object>>(401)
        .Produces<ApiResponse<object>>(404)
        .RequireAuthorization();
    }
}
