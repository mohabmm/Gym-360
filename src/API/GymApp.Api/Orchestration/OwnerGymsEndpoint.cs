using GymApp.Common.Infrastructure.Tenancy.Provisioning;
using GymApp.Common.Presentation;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace GymApp.Api.Orchestration;

internal sealed class OwnerGymsEndpoint : IEndpoint
{
    internal sealed record Request(Guid GymId, Guid OwnerId, string Name, string Slug);

    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost("api/v1/owners/{ownerId:guid}/gyms", async (
            Guid ownerId,
            Request request,
            IGymProvisioningService provisioning,
            CancellationToken ct) =>
        {
            if (request.OwnerId != ownerId)
                return Results.BadRequest(new { error = "OwnerId in body does not match route." });

            var registration = await provisioning.ProvisionAsync(
                request.GymId, ownerId, request.Name, request.Slug, gymCode: null, cancellationToken: ct);

            return Results.Ok(new
            {
                registration.GymId,
                registration.OwnerId,
                registration.Name,
                registration.Slug,
                registration.SchemaPrefix,
                registration.SequentialNumber,
                registration.Status,
                registration.CreatedAt
            });
        })
        .WithTags("Tenancy")
        .WithSummary("Provision a new gym for an owner (allocates per-gym schemas)")
        .WithDescription("Allocates the next sequential SchemaPrefix (g{N}), creates the per-gym schemas, and inserts a row in gyms_registry. Owner/Admin only.")
        .RequireAuthorization("gym_owner_or_admin");
    }
}
