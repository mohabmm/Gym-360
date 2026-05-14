using GymApp.Common.Presentation;
using GymApp.Modules.Gamification.Application.Commands.ModerateTestimonial;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace GymApp.Modules.Gamification.Presentation.Endpoints.Trainers;

internal sealed class ModerateTestimonialEndpoint : IEndpoint
{
    internal sealed record ModerateTestimonialRequest(
        Guid TestimonialId,
        Guid GymId,
        bool IsApproved,
        Guid ModeratedBy);

    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost("api/v1/gamification/trainers/{trainerId:guid}/testimonials", async (
            Guid trainerId,
            ModerateTestimonialRequest request,
            ISender sender) =>
        {
            var command = new ModerateTestimonialCommand(
                request.TestimonialId,
                trainerId,
                request.GymId,
                request.IsApproved,
                request.ModeratedBy);

            var result = await sender.Send(command);

            return result.IsSuccess
                ? Results.NoContent()
                : ApiResults.Problem(result.Error);
        })
        .RequireAuthorization()
        .WithSummary("Moderate a trainer testimonial")
        .WithDescription("Approves or rejects a client testimonial on a trainer's profile; requires the testimonial_moderation feature to be enabled for the gym.")
        .Produces(204)
        .WithTags("Gamification")
        .Produces<ApiResponse<object>>(400)
        .Produces<ApiResponse<object>>(401)
        .Produces<ApiResponse<object>>(403)
        .Produces<ApiResponse<object>>(404);
    }
}
