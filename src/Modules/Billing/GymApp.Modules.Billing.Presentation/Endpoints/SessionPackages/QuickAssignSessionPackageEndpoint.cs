using GymApp.Common.Presentation;
using GymApp.Modules.Billing.Application.Commands.SessionPackages;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace GymApp.Modules.Billing.Presentation.Endpoints.SessionPackages;

internal sealed class QuickAssignSessionPackageEndpoint : IEndpoint
{
    internal sealed record Request(
        Guid GymId,
        Guid TrainerId,
        Guid MemberId,
        int SessionCount,
        decimal? TotalPrice,
        string? PaymentMethod,
        DateTime? ExpiresAt,
        bool? IsTrial);

    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost("api/v1/billing/packages/quick-assign", async (
            Request request,
            HttpContext httpContext,
            ISender sender) =>
        {
            // Who assigned the package is the signed-in staff member, never a value from the body.
            if (!UserContext.TryGetUserId(httpContext, out var assignedBy))
                return Results.Unauthorized();

            var command = new QuickAssignSessionPackageCommand(
                request.GymId,
                request.TrainerId,
                request.MemberId,
                request.SessionCount,
                assignedBy,
                request.TotalPrice,
                request.PaymentMethod,
                request.ExpiresAt,
                request.IsTrial ?? false);

            var result = await sender.Send(command);

            return result.IsSuccess
                ? ApiResults.Created(result.Value, $"api/v1/billing/packages/{result.Value}")
                : ApiResults.Problem(result.Error);
        })
        // Owner / manager / receptionist can all assign trainers to members
        // per product spec. Sub-role checks belong on the FE; the BE policy
        // gates the principal type so a trainee or trainer cannot grant
        // themselves a free package.
        .RequireAuthorization("gym_staff")
        .WithTags("Billing")
        .WithSummary("Quick-assign a trainer to a member with any session count (1–100, free or paid), or a free 1–20 session trial.")
        .WithDescription("Creates an Active SessionPackage with the given SessionCount (1..100), no template required. Used by the staff app Add Member flow and the staff app/web 'Assign Trainer' flow on the member detail screen. TotalPrice is the lump-sum package price (per-session is derived); leave null for free assignments.")
        .Produces<ApiResponse<Guid>>(201)
        .Produces<ApiResponse<object>>(400)
        .Produces<ApiResponse<object>>(401)
        .Produces<ApiResponse<object>>(403);
    }
}
