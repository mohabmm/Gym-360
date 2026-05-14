using GymApp.Common.Presentation;
using GymApp.Modules.Gyms.Application.Abstractions;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace GymApp.Modules.Gyms.Presentation.Endpoints;

internal sealed class DownloadImportTemplateEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet("api/v1/gyms/imports/template.xlsx",
            async (IImportTemplateProvider provider, CancellationToken ct) =>
        {
            var stream = await provider.GenerateAsync(ct);
            return Results.File(stream,
                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                "gymapp-import-template.xlsx");
        })
        .WithTags("Gyms")
        .WithSummary("Download the Excel import template")
        .RequireAuthorization();
    }
}
