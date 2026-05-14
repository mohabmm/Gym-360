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
        Guid AssignedBy,
        decimal? TotalPrice,
        string? PaymentMethod,
        DateTime? ExpiresAt);

    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost("api/v1/billing/packages/quick-assign", async (
            Request request,
            ISender sender) =>
        {
            var command = new QuickAssignSessionPackageCommand(
                request.GymId,
                request.TrainerId,
                request.MemberId,
                request.SessionCount,
                request.AssignedBy,
                request.TotalPrice,
                request.PaymentMethod,
                request.ExpiresAt);

            var result = await sender.Send(command);

            return result.IsSuccess
                ? ApiResults.Created(result.Value, $"api/v1/billing/packages/{result.Value}")
                : ApiResults.Problem(result.Error);
        })
        // Owner / manager / receptionist can all assign trainers to members
        // per product spec (admin-or-staff-or-owner). Hardcoded staff sub-role
        // checks belong on the FE; the BE policy gates the principal type.
        .RequireAuthorization()
        .WithTags("Billing")
        .WithSummary("Quick-assign a trainer to a member with a session count (multiple of 4).")
        .WithDescription("Creates an Active SessionPackage with the given SessionCount (multiples of 4, 4..40), no template required. Used by the staff app/web 'Assign Trainer' flow on the member detail screen. TotalPrice is the lump-sum package price (per-session is derived); leave null for free assignments.")
        .Produces<ApiResponse<Guid>>(201)
        .Produces<ApiResponse<object>>(400)
        .Produces<ApiResponse<object>>(401)
        .Produces<ApiResponse<object>>(403);
    }
}
