using GymApp.Common.Presentation;
using GymApp.Modules.Gyms.Application.Commands.AddMemberNote;
using GymApp.Modules.Gyms.Application.Queries.GetMemberNotes;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace GymApp.Modules.Gyms.Presentation.Endpoints;

internal sealed class AddMemberNoteEndpoint : IEndpoint
{
    internal sealed record Request(string Content);

    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost("api/v1/gyms/{gymId:guid}/members/{memberId:guid}/notes",
            async (Guid gymId, Guid memberId, Request request, HttpContext httpContext, ISender sender) =>
        {
            var userId = httpContext.User.FindFirst("sub")?.Value
                      ?? httpContext.User.FindFirst("http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier")?.Value;

            if (!Guid.TryParse(userId, out var createdBy))
                return Results.Unauthorized();

            var command = new AddMemberNoteCommand(
                gymId,
                memberId,
                createdBy,
                request.Content);

            var result = await sender.Send(command);

            return result.IsSuccess
                ? ApiResults.Created(result.Value)
                : ApiResults.Problem(result.Error);
        })
        .WithTags("Gyms")
        .WithSummary("Add a note about a gym member")
        .WithDescription("Creates a private trainer/staff note about a gym member.")
        .Produces<ApiResponse<Guid>>(201)
        .Produces<ApiResponse<object>>(400)
        .Produces<ApiResponse<object>>(401)
        .RequireAuthorization();

        app.MapGet("api/v1/gyms/{gymId:guid}/members/{memberId:guid}/notes",
            async (Guid gymId, Guid memberId, ISender sender) =>
        {
            var query = new GetMemberNotesQuery(gymId, memberId);
            var result = await sender.Send(query);

            return result.IsSuccess
                ? Results.Ok(new ApiResponse<List<MemberNoteDto>>(result.Value))
                : ApiResults.Problem(result.Error);
        })
        .WithTags("Gyms")
        .WithSummary("Get notes about a gym member")
        .WithDescription("Retrieves all private notes about a gym member.")
        .Produces<ApiResponse<List<MemberNoteDto>>>(200)
        .Produces<ApiResponse<object>>(401)
        .RequireAuthorization();
    }
}
