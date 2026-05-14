using GymApp.Common.Presentation;
using GymApp.Modules.Identity.Application.DTOs;
using GymApp.Modules.Identity.Application.Queries.AdminUsers;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace GymApp.Modules.Identity.Presentation.Endpoints;

internal sealed class GetAdminUserByIdEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet("api/v1/admin/users/{userId:guid}", async (Guid userId, ISender sender, HttpContext httpContext) =>
        {
            var query = new GetAdminUserByIdQuery(userId);
            var result = await sender.Send(query);
            if (result.IsFailure) return ApiResults.Problem(result.Error);

            var dto = result.Value;
            if (UserContext.TryGetUserId(httpContext, out var me))
                dto = dto with { IsYou = dto.Id == me };
            return ApiResults.Ok(dto);
        })
        .WithTags("Admin", "Identity")
        .WithSummary("Get admin user by id")
        .Produces<ApiResponse<AdminUserDto>>(200)
        .Produces<ApiResponse<object>>(401)
        .Produces<ApiResponse<object>>(403)
        .Produces<ApiResponse<object>>(404)
        .RequireAuthorization("platform_admin");
    }
}
