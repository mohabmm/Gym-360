using GymApp.Common.Presentation;
using GymApp.Modules.Gyms.Application.Commands.RecordCheckIn;
using GymApp.Modules.Gyms.Application.DTOs;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace GymApp.Modules.Gyms.Presentation.Endpoints;

internal sealed class RecordCheckInEndpoint : IEndpoint
{
    internal sealed record Request(
        string Method,
        string? QrToken,
        string? MemberNumber,
        Guid? MemberId,
        Guid? StaffId);

    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost("api/v1/gyms/{gymId:guid}/check-in", async (Guid gymId, Request request, ISender sender) =>
        {
            var command = new RecordCheckInCommand(
                gymId,
                request.Method,
                request.QrToken,
                request.MemberNumber,
                request.MemberId,
                request.StaffId);

            var result = await sender.Send(command);

            return result.IsSuccess
                ? ApiResults.Created(result.Value)
                : ApiResults.Problem(result.Error);
        })
        .WithTags("Gyms")
        .WithSummary("Record a member check-in")
        .WithDescription("Records a gym check-in via QR code, member number, or member ID. Supports kiosk mode and staff-assisted check-in methods.")
        .Produces<ApiResponse<CheckInResultDto>>(201)
        .Produces<ApiResponse<object>>(400)
        .Produces<ApiResponse<object>>(401)
        .Produces<ApiResponse<object>>(404)
        .RequireAuthorization();
    }
}
