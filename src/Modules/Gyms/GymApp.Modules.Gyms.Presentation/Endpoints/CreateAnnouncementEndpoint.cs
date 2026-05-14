using GymApp.Common.Presentation;
using GymApp.Modules.Gyms.Application.Commands.CreateAnnouncement;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace GymApp.Modules.Gyms.Presentation.Endpoints;

internal sealed class CreateAnnouncementEndpoint : IEndpoint
{
    internal sealed record Request(
        string Title,
        string Content,
        string? ImageUrl,
        string Category,
        string TargetAudience,
        DateTime? ScheduledAt);

    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost("api/v1/gyms/{gymId:guid}/announcements",
            async (Guid gymId, Request request, HttpContext httpContext, ISender sender) =>
        {
            var userId = httpContext.User.FindFirst("sub")?.Value
                      ?? httpContext.User.FindFirst("http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier")?.Value;

            if (!Guid.TryParse(userId, out var createdBy))
                return Results.Unauthorized();

            var command = new CreateAnnouncementCommand(
                gymId,
                request.Title,
                request.Content,
                request.ImageUrl,
                request.Category,
                request.TargetAudience,
                request.ScheduledAt,
                createdBy);

            var result = await sender.Send(command);

            return result.IsSuccess
                ? ApiResults.Created(result.Value)
                : ApiResults.Problem(result.Error);
        })
        .WithTags("Gyms")
        .WithSummary("Create a gym announcement")
        .WithDescription("Creates a new announcement for the gym. Announcements can be scheduled for future publication and targeted to specific audience groups.")
        .Produces<ApiResponse<Guid>>(201)
        .Produces<ApiResponse<object>>(400)
        .Produces<ApiResponse<object>>(401)
        .Produces<ApiResponse<object>>(404)
        .RequireAuthorization();
    }
}
