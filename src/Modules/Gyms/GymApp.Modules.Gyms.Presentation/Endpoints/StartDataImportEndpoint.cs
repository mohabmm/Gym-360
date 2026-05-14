using System.Security.Claims;
using GymApp.Common.Presentation;
using GymApp.Modules.Gyms.Application.Commands.StartDataImport;
using GymApp.Modules.Gyms.Domain.Enums;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace GymApp.Modules.Gyms.Presentation.Endpoints;

internal sealed class StartDataImportEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost("api/v1/gyms/{gymId:guid}/imports",
            async (Guid gymId, HttpRequest httpRequest, ClaimsPrincipal user, ISender sender, CancellationToken ct) =>
        {
            if (!httpRequest.HasFormContentType)
                return ApiResults.Problem("DataImport.InvalidContentType", "Request must be multipart/form-data.", 400);

            var form = await httpRequest.ReadFormAsync(ct);
            var file = form.Files["file"];
            if (file is null || file.Length == 0)
                return ApiResults.Problem("DataImport.MissingFile", "A non-empty 'file' field is required.", 400);

            if (!Enum.TryParse<DataImportSource>(form["source"].ToString(), ignoreCase: true, out var source))
                source = DataImportSource.Excel;

            var userIdClaim = user.FindFirstValue(ClaimTypes.NameIdentifier) ?? user.FindFirstValue("sub");
            if (!Guid.TryParse(userIdClaim, out var userId))
                return ApiResults.Problem("Auth.Unauthorized", "Valid user claim required.", 401);

            await using var content = file.OpenReadStream();
            var command = new StartDataImportCommand(
                gymId, userId, source, file.FileName, file.Length, content);

            var result = await sender.Send(command, ct);

            return result.IsSuccess
                ? ApiResults.Created(result.Value)
                : ApiResults.Problem(result.Error);
        })
        .WithTags("Gyms")
        .WithSummary("Start a data import job")
        .WithDescription("Uploads an Excel workbook exported from Odoo, Daftra, or manually prepared. Processes members, payments, session packages, and InBody records. Pro plan and above.")
        .DisableAntiforgery()
        .Produces<ApiResponse<Guid>>(201)
        .Produces<ApiResponse<object>>(400)
        .Produces<ApiResponse<object>>(401)
        .Produces<ApiResponse<object>>(403)
        .Produces<ApiResponse<object>>(404)
        .RequireAuthorization();
    }
}
