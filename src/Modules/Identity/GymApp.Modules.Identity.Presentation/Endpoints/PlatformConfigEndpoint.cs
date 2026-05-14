using GymApp.Common.Presentation;
using GymApp.Modules.Identity.Application.Abstractions;
using GymApp.Modules.Identity.Application.Commands.PlatformSettings;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace GymApp.Modules.Identity.Presentation.Endpoints;

internal sealed class PlatformConfigEndpoint : IEndpoint
{
    internal sealed record Request(
        string? PlatformName,
        string? DefaultLanguage,
        string? DefaultCurrency,
        string? SupportEmail);

    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet("api/v1/admin/platform-config", async (IPlatformSettingsRepository repo, CancellationToken ct) =>
        {
            var all = await repo.GetAllAsync(ct);
            string? Get(string k) => all.FirstOrDefault(s => s.Key == k)?.Value;
            var dto = new
            {
                platformName = Get("platform.name") ?? "Gym 360",
                defaultLanguage = Get("platform.defaultLanguage") ?? "ar",
                defaultCurrency = Get("platform.defaultCurrency") ?? "EGP",
                supportEmail = Get("platform.supportEmail") ?? "support@gym360.com",
            };
            return Results.Ok(new ApiResponse<object>(dto));
        })
        .WithTags("Admin", "Settings")
        .WithSummary("Get platform configuration")
        .RequireAuthorization("platform_admin");

        app.MapMethods("api/v1/admin/platform-config", new[] { "PATCH" }, async (
            Request request,
            ISender sender,
            HttpContext httpContext) =>
        {
            var updatedBy = UserContext.TryGetUserId(httpContext, out var me) ? me : (Guid?)null;
            var values = new Dictionary<string, string?>();
            if (request.PlatformName is not null) values["platform.name"] = request.PlatformName;
            if (request.DefaultLanguage is not null) values["platform.defaultLanguage"] = request.DefaultLanguage;
            if (request.DefaultCurrency is not null) values["platform.defaultCurrency"] = request.DefaultCurrency;
            if (request.SupportEmail is not null) values["platform.supportEmail"] = request.SupportEmail;

            var result = await sender.Send(new UpdatePlatformSettingsCommand(values, updatedBy));
            return result.IsSuccess
                ? Results.NoContent()
                : ApiResults.Problem(result.Error);
        })
        .WithTags("Admin", "Settings")
        .WithSummary("Update platform configuration")
        .WithDescription("Updates platform-wide configuration (name, language, currency, support email). Requires platform_admin.")
        .Produces(204)
        .Produces<ApiResponse<object>>(401)
        .Produces<ApiResponse<object>>(403)
        .RequireAuthorization("platform_admin");
    }
}

internal sealed class SecuritySettingsEndpoint : IEndpoint
{
    internal sealed record Request(
        bool? TwoFactorRequired,
        bool? IpWhitelistEnabled);

    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet("api/v1/admin/security-settings", async (IPlatformSettingsRepository repo, CancellationToken ct) =>
        {
            var all = await repo.GetAllAsync(ct);
            bool ParseBool(string k, bool fallback)
                => bool.TryParse(all.FirstOrDefault(s => s.Key == k)?.Value, out var v) ? v : fallback;
            var dto = new
            {
                twoFactorRequired = ParseBool("security.twoFactorRequired", true),
                ipWhitelistEnabled = ParseBool("security.ipWhitelistEnabled", false),
            };
            return Results.Ok(new ApiResponse<object>(dto));
        })
        .WithTags("Admin", "Settings")
        .WithSummary("Get security settings")
        .RequireAuthorization("platform_admin");

        app.MapMethods("api/v1/admin/security-settings", new[] { "PATCH" }, async (
            Request request,
            ISender sender,
            HttpContext httpContext) =>
        {
            var updatedBy = UserContext.TryGetUserId(httpContext, out var me) ? me : (Guid?)null;
            var values = new Dictionary<string, string?>();
            if (request.TwoFactorRequired.HasValue)
                values["security.twoFactorRequired"] = request.TwoFactorRequired.Value ? "true" : "false";
            if (request.IpWhitelistEnabled.HasValue)
                values["security.ipWhitelistEnabled"] = request.IpWhitelistEnabled.Value ? "true" : "false";

            var result = await sender.Send(new UpdatePlatformSettingsCommand(values, updatedBy));
            return result.IsSuccess
                ? Results.NoContent()
                : ApiResults.Problem(result.Error);
        })
        .WithTags("Admin", "Settings")
        .WithSummary("Update security settings")
        .WithDescription("Toggles 2FA requirement and IP whitelisting for admin access. Requires platform_admin.")
        .Produces(204)
        .Produces<ApiResponse<object>>(401)
        .Produces<ApiResponse<object>>(403)
        .RequireAuthorization("platform_admin");
    }
}
