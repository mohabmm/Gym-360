using GymApp.Api.Hubs;
using GymApp.Api.Middleware;
using GymApp.Common.Presentation;
using Serilog;

namespace GymApp.Api.Extensions;

/// <summary>
/// The HTTP middleware pipeline and endpoint mapping. Order is load-bearing —
/// see the inline notes (ForwardedHeaders first, CORS before the exception handler).
/// </summary>
public static class PipelineExtensions
{
    public static WebApplication UseApiPipeline(this WebApplication app)
    {
        // --- Pipeline ---
        // ForwardedHeaders FIRST: behind Dokploy/Traefik (or any reverse proxy), trust
        // X-Forwarded-Proto / X-Forwarded-For so Request.Scheme, Url.Action redirects,
        // and client-IP logging reflect the real client. Must run before auth/CORS.
        {
            var fwd = new Microsoft.AspNetCore.Builder.ForwardedHeadersOptions
            {
                ForwardedHeaders = Microsoft.AspNetCore.HttpOverrides.ForwardedHeaders.XForwardedFor
                    | Microsoft.AspNetCore.HttpOverrides.ForwardedHeaders.XForwardedProto
                    | Microsoft.AspNetCore.HttpOverrides.ForwardedHeaders.XForwardedHost,
            };
            fwd.KnownNetworks.Clear();
            fwd.KnownProxies.Clear();
            app.UseForwardedHeaders(fwd);
        }

        // UseCors must run before UseExceptionHandler so error responses keep CORS headers;
        // otherwise the browser surfaces a misleading "blocked by CORS" error masking the real 5xx.
        app.UseCors();
        app.UseExceptionHandler();
        app.UseSerilogRequestLogging();

        if (app.Environment.IsDevelopment())
        {
            app.UseSwagger();
            app.UseSwaggerUI();
            app.MapGet("/", () => Results.Redirect("/swagger")).ExcludeFromDescription();
        }

        app.UseAuthentication();
        app.UseAuthorization();

        // --- Tenant resolution (no-op when Tenancy:Mode=Soft) ---
        app.UseMiddleware<TenantResolutionMiddleware>();

        // --- Health check endpoints ---
        app.MapHealthChecks("/health/live");
        app.MapHealthChecks("/health/ready", new Microsoft.AspNetCore.Diagnostics.HealthChecks.HealthCheckOptions
        {
            Predicate = check => check.Tags.Contains("ready")
        });

        // --- SignalR hubs ---
        app.MapHub<ConfigHub>(ConfigHub.HubPath);
        app.MapHub<ChatHub>(ChatHub.HubPath);

        // --- Map module endpoints ---
        var endpointTypes = AppDomain.CurrentDomain.GetAssemblies()
            .Where(a => a.FullName?.StartsWith("GymApp") == true)
            .SelectMany(a => a.GetTypes())
            .Where(t => typeof(IEndpoint).IsAssignableFrom(t) && !t.IsInterface && !t.IsAbstract);

        foreach (var endpointType in endpointTypes)
        {
            var endpoint = (IEndpoint)Activator.CreateInstance(endpointType)!;
            endpoint.MapEndpoint(app);
        }

        return app;
    }
}
