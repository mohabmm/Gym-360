using GymApp.Common.Presentation;
using GymApp.Modules.Identity.Application.Commands.AdminUsers;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace GymApp.Modules.Identity.Presentation.Endpoints;

internal sealed class UpdateAdminUserEndpoint : IEndpoint
{
    internal sealed record Request(string? FirstName, string? LastName, string? Phone);

    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapMethods("api/v1/admin/users/{userId:guid}", new[] { "PATCH", "PUT" },
            async (Guid userId, Request request, ISender sender) =>
            {
                var command = new UpdateAdminUserCommand(userId, request.FirstName, request.LastName, request.Phone);
                var result = await sender.Send(command);
                return result.IsSuccess
                    ? Results.Ok(new ApiResponse<object>(null))
                    : ApiResults.Problem(result.Error);
            })
            .WithTags("Admin", "Identity")
            .WithSummary("Update admin user profile")
            .Produces<ApiResponse<object>>(200)
            .Produces<ApiResponse<object>>(401)
            .Produces<ApiResponse<object>>(403)
            .Produces<ApiResponse<object>>(404)
            .RequireAuthorization("platform_admin");
    }
}
