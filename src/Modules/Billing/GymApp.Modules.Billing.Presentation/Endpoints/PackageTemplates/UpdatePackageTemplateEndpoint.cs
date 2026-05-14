using GymApp.Common.Presentation;
using GymApp.Modules.Billing.Application.Commands.PackageTemplates;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace GymApp.Modules.Billing.Presentation.Endpoints.PackageTemplates;

internal sealed class UpdatePackageTemplateEndpoint : IEndpoint
{
    internal sealed record Request(
        string? Name,
        int? SessionCount,
        decimal? PricePerSession,
        bool? IsActive);

    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPut("api/v1/billing/package-templates/{id:guid}", async (Guid id, Request request, ISender sender) =>
        {
            var command = new UpdatePackageTemplateCommand(
                id, request.Name, request.SessionCount, request.PricePerSession, request.IsActive);
            var result = await sender.Send(command);
            return result.IsSuccess
                ? Results.Ok(new ApiResponse<object>(null))
                : ApiResults.Problem(result.Error);
        })
        .RequireAuthorization()
        .WithSummary("Update a session package template")
        .WithTags("Billing")
        .Produces<ApiResponse<object>>(200)
        .Produces<ApiResponse<object>>(400)
        .Produces<ApiResponse<object>>(401)
        .Produces<ApiResponse<object>>(404);
    }
}
