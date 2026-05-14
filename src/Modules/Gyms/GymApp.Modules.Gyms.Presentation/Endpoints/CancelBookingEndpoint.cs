using GymApp.Common.Presentation;
using GymApp.Modules.Gyms.Application.Commands.CancelBooking;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace GymApp.Modules.Gyms.Presentation.Endpoints;

internal sealed class CancelBookingEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapDelete("api/v1/gyms/{gymId:guid}/classes/{classId:guid}/bookings/{bookingId:guid}", async (
            Guid gymId,
            Guid classId,
            Guid bookingId,
            ISender sender) =>
        {
            var command = new CancelBookingCommand(gymId, classId, bookingId);

            var result = await sender.Send(command);

            return result.IsSuccess
                ? Results.NoContent()
                : ApiResults.Problem(result.Error);
        })
        .WithTags("Gyms")
        .WithSummary("Cancel a class booking")
        .WithDescription("Cancels an existing class booking. Late cancellations within the gym's configured cancellation window will deduct a session from the member's package.")
        .Produces(204)
        .Produces<ApiResponse<object>>(401)
        .Produces<ApiResponse<object>>(404)
        .RequireAuthorization();
    }
}
