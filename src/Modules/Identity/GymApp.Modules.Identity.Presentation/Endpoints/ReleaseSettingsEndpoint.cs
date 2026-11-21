using System.Security.Cryptography;
using System.Text;
using GymApp.Common.Presentation;
using GymApp.Common.Presentation.FeatureFlags;
using GymApp.Modules.Identity.Application.Abstractions;
using GymApp.Modules.Identity.Application.Commands.PlatformSettings;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace GymApp.Modules.Identity.Presentation.Endpoints;

/// <summary>
/// Release automation switches. Platform admins flip them in the Main Portal; the
/// Codemagic Android workflows read them at build start (with the shared CI token)
/// and cancel themselves when auto-publish to Play Internal testing is off.
/// </summary>
internal sealed class ReleaseSettingsEndpoint : IEndpoint
{
    internal const string AndroidInternalAutoPublishKey = "ci.androidInternalAutoPublish";
    internal const string CiTokenHeader = "X-Ci-Token";
    internal const string CiTokenConfigKey = "Ci:ReleaseToken";

    internal sealed record Request(bool? AndroidInternalAutoPublish);

    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet("api/v1/admin/release-settings", async (IPlatformSettingsRepository repo, CancellationToken ct) =>
        {
            var setting = (await repo.GetAllAsync(ct)).FirstOrDefault(s => s.Key == AndroidInternalAutoPublishKey);
            var dto = new
            {
                androidInternalAutoPublish = ParseEnabled(setting?.Value),
                updatedAt = setting?.UpdatedAt,
            };
            return Results.Ok(new ApiResponse<object>(dto));
        })
        .WithTags("Admin", "Settings")
        .WithSummary("Get release automation settings")
        .RequireAuthorization("platform_admin");

        app.MapMethods("api/v1/admin/release-settings", new[] { "PATCH" }, async (
            Request request,
            ISender sender,
            HttpContext httpContext) =>
        {
            if (request.AndroidInternalAutoPublish is null)
                return Results.NoContent();

            var updatedBy = UserContext.TryGetUserId(httpContext, out var me) ? me : (Guid?)null;
            var values = new Dictionary<string, string?>
            {
                [AndroidInternalAutoPublishKey] = request.AndroidInternalAutoPublish.Value ? "true" : "false",
            };

            var result = await sender.Send(new UpdatePlatformSettingsCommand(values, updatedBy));
            return result.IsSuccess
                ? Results.NoContent()
                : ApiResults.Problem(result.Error);
        })
        .WithTags("Admin", "Settings")
        .WithSummary("Update release automation settings")
        .WithDescription("Turns automatic Android uploads to the Google Play Internal testing track on or off. Requires platform_admin.")
        .Produces(204)
        .Produces<ApiResponse<object>>(401)
        .Produces<ApiResponse<object>>(403)
        .RequireAuthorization("platform_admin");

        app.MapGet("api/v1/ci/release-settings", async (IPlatformSettingsRepository repo, CancellationToken ct) =>
        {
            var setting = (await repo.GetAllAsync(ct)).FirstOrDefault(s => s.Key == AndroidInternalAutoPublishKey);
            return Results.Ok(new ApiResponse<object>(new { androidInternalAutoPublish = ParseEnabled(setting?.Value) }));
        })
        .WithTags("CI")
        .WithSummary("Release automation switches for the CI pipeline")
        .WithDescription($"Read by Codemagic before each Android build. Requires the {CiTokenHeader} header.")
        .AllowAnonymous()
        // Platform pipeline, not a gym feature — never plan-gated.
        .AlwaysOn()
        .AddEndpointFilter(RequireCiToken);
    }

    /// <summary>Auto-publish is on unless an admin has explicitly switched it off.</summary>
    private static bool ParseEnabled(string? value) => !bool.TryParse(value, out var enabled) || enabled;

    private static async ValueTask<object?> RequireCiToken(EndpointFilterInvocationContext context, EndpointFilterDelegate next)
    {
        var http = context.HttpContext;
        var expected = http.RequestServices.GetRequiredService<IConfiguration>()[CiTokenConfigKey];
        if (string.IsNullOrWhiteSpace(expected))
            return ApiResults.Problem("CI_TOKEN_NOT_CONFIGURED", "The CI release token is not configured on this server.", 503);

        var supplied = http.Request.Headers[CiTokenHeader].ToString();
        if (!TokensMatch(supplied, expected))
            return ApiResults.Problem("UNAUTHORIZED", "Missing or invalid CI token.", 401);

        return await next(context);
    }

    private static bool TokensMatch(string supplied, string expected) =>
        CryptographicOperations.FixedTimeEquals(Encoding.UTF8.GetBytes(supplied), Encoding.UTF8.GetBytes(expected));
}
