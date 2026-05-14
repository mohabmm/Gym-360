using GymApp.Common.Presentation;
using GymApp.Modules.Identity.Application.Commands.AdminUsers;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace GymApp.Modules.Identity.Presentation.Endpoints;

internal sealed class CreateAdminUserEndpoint : IEndpoint
{
    internal sealed record Request(
        string Email,
        string? Password,
        string FirstName,
        string LastName,
        string AdminRole);

    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost("api/v1/admin/users", async (Request request, ISender sender, HttpContext httpContext) =>
        {
            // Default to a strong server-generated password if the caller doesn't provide one.
            // In practice the admin UI should prompt the operator, but we keep this fallback so
            // the endpoint never silently fails on missing input.
            var password = string.IsNullOrWhiteSpace(request.Password)
                ? Guid.NewGuid().ToString("N") + "!Aa1"
                : request.Password;

            Guid? createdBy = UserContext.TryGetUserId(httpContext, out var me) ? me : null;

            var command = new CreateAdminUserCommand(
                request.Email,
                password,
                request.FirstName,
                request.LastName,
                request.AdminRole,
                createdBy);

            var result = await sender.Send(command);

            return result.IsSuccess
                ? ApiResults.Created(result.Value, $"api/v1/admin/users/{result.Value}")
                : ApiResults.Problem(result.Error);
        })
        .WithTags("Admin", "Identity")
        .WithSummary("Create an admin user")
        .WithDescription("Creates a platform admin with the specified admin role. Requires platform_admin.")
        .Produces<ApiResponse<Guid>>(201)
        .Produces<ApiResponse<object>>(400)
        .Produces<ApiResponse<object>>(401)
        .Produces<ApiResponse<object>>(403)
        .RequireAuthorization("platform_admin");
    }
}
