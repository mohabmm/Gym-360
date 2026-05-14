using GymApp.Common.Presentation;
using GymApp.Modules.Gyms.Application.Commands.BookClass;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace GymApp.Modules.Gyms.Presentation.Endpoints;

internal sealed class BookClassEndpoint : IEndpoint
{
    internal sealed record Request();

    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost("api/v1/gyms/{gymId:guid}/classes/{classId:guid}/book", async (
            Guid gymId,
            Guid classId,
            HttpContext httpContext,
            ISender sender) =>
        {
            if (!CallerContext.TryGetUserId(httpContext, out var memberId))
                return Results.Unauthorized();

            var command = new BookClassCommand(gymId, classId, memberId);

            var result = await sender.Send(command);

            return result.IsSuccess
                ? Results.Ok(new ApiResponse<object>(null))
                : ApiResults.Problem(result.Error);
        })
        .WithTags("Gyms")
        .WithSummary("Book a member into a class")
        .WithDescription("Books a member into a gym class. If the class is at capacity and waitlist is enabled, the member is added to the waitlist.")
        .Produces<ApiResponse<object>>(200)
        .Produces<ApiResponse<object>>(400)
        .Produces<ApiResponse<object>>(401)
        .Produces<ApiResponse<object>>(404)
        .RequireAuthorization();
    }
}
