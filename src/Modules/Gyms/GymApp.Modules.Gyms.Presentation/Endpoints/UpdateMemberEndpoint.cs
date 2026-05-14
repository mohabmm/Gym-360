using GymApp.Common.Presentation;
using GymApp.Modules.Gyms.Application.Commands.UpdateMember;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace GymApp.Modules.Gyms.Presentation.Endpoints;

internal sealed class UpdateMemberEndpoint : IEndpoint
{
    internal sealed record Request(string? Name, string? Phone, string? Section);

    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapMethods("api/v1/gyms/{gymId:guid}/members/{memberId:guid}", new[] { "PATCH", "PUT" },
            async (Guid gymId, Guid memberId, Request request, ISender sender) =>
            {
                var command = new UpdateMemberCommand(gymId, memberId, request.Name, request.Phone, request.Section);
                var result = await sender.Send(command);
                return result.IsSuccess
                    ? Results.Ok(new ApiResponse<object>(null))
                    : ApiResults.Problem(result.Error);
            })
            .RequireAuthorization()
            .WithSummary("Update a gym member profile (name/phone/section)")
            .WithTags("Gyms")
            .Produces<ApiResponse<object>>(200)
            .Produces<ApiResponse<object>>(400)
            .Produces<ApiResponse<object>>(401)
            .Produces<ApiResponse<object>>(404);
    }
}
