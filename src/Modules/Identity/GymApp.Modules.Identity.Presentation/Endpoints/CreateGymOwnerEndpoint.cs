using GymApp.Common.Presentation;
using GymApp.Modules.Identity.Application.Commands.Owners;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace GymApp.Modules.Identity.Presentation.Endpoints;

internal sealed class CreateGymOwnerEndpoint : IEndpoint
{
    internal sealed record Request(
        string Email,
        string Password,
        string FirstName,
        string LastName,
        string? Phone,
        string CompanyName,
        string BillingEmail,
        string? PlanTier,
        string? DashboardMode);

    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost("api/v1/admin/owners", async (Request request, ISender sender) =>
        {
            var command = new CreateGymOwnerCommand(
                request.Email,
                request.Password,
                request.FirstName,
                request.LastName,
                request.Phone,
                request.CompanyName,
                request.BillingEmail,
                string.IsNullOrWhiteSpace(request.PlanTier) ? "Pro" : request.PlanTier,
                string.IsNullOrWhiteSpace(request.DashboardMode) ? "Both" : request.DashboardMode);

            var result = await sender.Send(command);

            return result.IsSuccess
                ? ApiResults.Created(result.Value, $"api/v1/admin/owners/{result.Value.OwnerId}")
                : ApiResults.Problem(result.Error);
        })
        .WithTags("Admin", "Identity")
        .WithSummary("Create a gym owner principal")
        .WithDescription("Provisions a User (role=GymOwner, activated), an Owner aggregate, and a Contract row, then links users.owner_id. Gyms can be added afterwards via POST /owners/{id}/gyms. Requires platform_admin.")
        .Produces<ApiResponse<CreateGymOwnerResult>>(201)
        .Produces<ApiResponse<object>>(400)
        .Produces<ApiResponse<object>>(401)
        .Produces<ApiResponse<object>>(403)
        .RequireAuthorization("platform_admin");
    }
}
