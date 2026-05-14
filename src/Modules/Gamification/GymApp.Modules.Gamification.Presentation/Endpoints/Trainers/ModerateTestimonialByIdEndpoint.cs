using GymApp.Common.Presentation;
using GymApp.Modules.Gamification.Application.Commands.ModerateTestimonial;
using GymApp.Modules.Gamification.Domain.Repositories;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace GymApp.Modules.Gamification.Presentation.Endpoints.Trainers;

internal sealed class ModerateTestimonialByIdEndpoint : IEndpoint
{
    internal sealed record Request(string Status);

    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPut("api/v1/gamification/testimonials/{id:guid}/moderate", async (
            Guid id,
            Request request,
            ITrainerPublicProfileRepository profileRepository,
            HttpContext httpContext,
            ISender sender,
            CancellationToken ct) =>
        {
            if (!UserContext.TryGetUserId(httpContext, out var moderatedBy))
                return Results.Unauthorized();

            var profile = await profileRepository.FindByTestimonialIdAsync(id, ct);
            if (profile is null)
                return ApiResults.Problem("Gamification.TestimonialNotFound", "Testimonial not found.", 404);

            var isApproved = string.Equals(request.Status, "approved", StringComparison.OrdinalIgnoreCase);
            var command = new ModerateTestimonialCommand(id, profile.TrainerId, profile.GymId, isApproved, moderatedBy);
            var result = await sender.Send(command, ct);

            return result.IsSuccess
                ? Results.NoContent()
                : ApiResults.Problem(result.Error);
        })
        .RequireAuthorization()
        .WithSummary("Moderate testimonial by id (approve/reject)")
        .WithTags("Gamification")
        .Produces(204)
        .Produces<ApiResponse<object>>(400)
        .Produces<ApiResponse<object>>(401)
        .Produces<ApiResponse<object>>(403)
        .Produces<ApiResponse<object>>(404);
    }
}
