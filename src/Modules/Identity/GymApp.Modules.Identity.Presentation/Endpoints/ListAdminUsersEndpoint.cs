using GymApp.Common.Presentation;
using GymApp.Modules.Identity.Application.DTOs;
using GymApp.Modules.Identity.Application.Queries.AdminUsers;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace GymApp.Modules.Identity.Presentation.Endpoints;

internal sealed class ListAdminUsersEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet("api/v1/admin/users", async (string? role, ISender sender, HttpContext httpContext) =>
        {
            var query = new ListUsersQuery(role);
            var result = await sender.Send(query);

            if (result.IsFailure)
                return ApiResults.Problem(result.Error);

            // Stamp `isYou` so the UI can hide destructive actions on the caller's own row.
            if (UserContext.TryGetUserId(httpContext, out var me))
            {
                var stamped = result.Value.Select(u => u with { IsYou = u.Id == me }).ToList();
                return ApiResults.Ok(stamped);
            }

            return ApiResults.Ok(result.Value);
        })
        .WithTags("Admin", "Identity")
        .WithSummary("List users")
        .WithDescription("Returns all users across the platform with their role, status, and (when applicable) admin role. Filterable by role. Requires platform_admin.")
        .Produces<ApiResponse<List<AdminUserDto>>>(200)
        .Produces<ApiResponse<object>>(401)
        .Produces<ApiResponse<object>>(403)
        .RequireAuthorization("platform_admin");
    }
}
