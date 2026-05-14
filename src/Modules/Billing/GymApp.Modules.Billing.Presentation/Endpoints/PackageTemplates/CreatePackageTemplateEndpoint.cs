using GymApp.Common.Presentation;
using GymApp.Modules.Billing.Application.Commands.PackageTemplates;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace GymApp.Modules.Billing.Presentation.Endpoints.PackageTemplates;

internal sealed class CreatePackageTemplateEndpoint : IEndpoint
{
    internal sealed record CreatePackageTemplateRequest(
        Guid GymId,
        string Name,
        int SessionCount,
        decimal PricePerSession);

    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost("api/v1/billing/package-templates", async (
            CreatePackageTemplateRequest request,
            ISender sender) =>
        {
            var command = new CreatePackageTemplateCommand(
                request.GymId,
                request.Name,
                request.SessionCount,
                request.PricePerSession);

            var result = await sender.Send(command);

            return result.IsSuccess
                ? ApiResults.Created(result.Value, $"api/v1/billing/package-templates/{result.Value}")
                : ApiResults.Problem(result.Error);
        })
        .RequireAuthorization()
        .WithSummary("Create a session package template")
        .WithDescription("Creates a reusable session package template for a gym, defining session count and per-session price.")
        .WithTags("Billing")
        .Produces<ApiResponse<Guid>>(201)
        .Produces<ApiResponse<object>>(400)
        .Produces<ApiResponse<object>>(401);
    }
}
