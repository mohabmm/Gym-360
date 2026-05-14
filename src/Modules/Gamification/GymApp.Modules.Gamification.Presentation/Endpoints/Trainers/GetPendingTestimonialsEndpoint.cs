using GymApp.Common.Presentation;
using GymApp.Modules.Gamification.Application.DTOs;
using GymApp.Modules.Gamification.Application.Queries.GetPendingTestimonials;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace GymApp.Modules.Gamification.Presentation.Endpoints.Trainers;

internal sealed class GetPendingTestimonialsEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet("api/v1/gamification/testimonials/pending", async (
            Guid gymId,
            ISender sender) =>
        {
            var query = new GetPendingTestimonialsQuery(gymId);
            var result = await sender.Send(query);

            return result.IsSuccess
                ? ApiResults.Ok(result.Value)
                : ApiResults.Problem(result.Error);
        })
        .RequireAuthorization()
        .WithSummary("List pending (unmoderated) testimonials in a gym")
        .WithDescription("Returns all testimonials awaiting moderation across every trainer in the gym. Requires the testimonial_moderation feature.")
        .WithTags("Gamification")
        .Produces<ApiResponse<List<PendingTestimonialDto>>>(200)
        .Produces<ApiResponse<object>>(401)
        .Produces<ApiResponse<object>>(403);
    }
}
