using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

namespace GymApp.Api.Extensions;

/// <summary>
/// OpenTelemetry traces + metrics, exported via OTLP to a self-hosted SigNoz
/// collector. Logs are shipped separately through the Serilog OpenTelemetry sink
/// (configured in appsettings "Serilog" → see appsettings.Production/Staging).
///
/// Wiring is GATED on an OTLP endpoint being configured: with no
/// OTEL_EXPORTER_OTLP_ENDPOINT (or Otel:Endpoint) set, OpenTelemetry is not
/// registered at all, so local dev and tests are unaffected.
///
/// Env (set in Dokploy on the API app):
///   OTEL_EXPORTER_OTLP_ENDPOINT=http://signoz-otel-collector:4317
///   OTEL_EXPORTER_OTLP_PROTOCOL=grpc           (default; "http/protobuf" also supported)
///   OTEL_SERVICE_NAME=gymapp-api               (falls back to "gymapp-api")
/// The OTLP exporter reads these standard env vars natively.
/// </summary>
public static class ObservabilityExtensions
{
    public static WebApplicationBuilder AddObservability(this WebApplicationBuilder builder)
    {
        // Honor both the standard OTEL env var and an appsettings fallback.
        var endpoint = builder.Configuration["OTEL_EXPORTER_OTLP_ENDPOINT"]
                       ?? builder.Configuration["Otel:Endpoint"];

        if (string.IsNullOrWhiteSpace(endpoint))
            return builder; // OTel disabled — no collector configured (local/dev/test).

        var serviceName = builder.Configuration["OTEL_SERVICE_NAME"]
                          ?? builder.Configuration["Otel:ServiceName"]
                          ?? "gymapp-api";

        builder.Services.AddOpenTelemetry()
            .ConfigureResource(r => r.AddService(
                serviceName: serviceName,
                serviceVersion: typeof(ObservabilityExtensions).Assembly.GetName().Version?.ToString() ?? "1.0.0"))
            .WithTracing(tracing => tracing
                .AddAspNetCoreInstrumentation(o => o.RecordException = true)
                .AddHttpClientInstrumentation()
                .AddSqlClientInstrumentation(o =>
                {
                    // Record the exception on the SQL span so per-gym write failures
                    // (e.g. "Invalid column name 'RetryCount'" on g{N}_outbox_messages)
                    // surface as span errors in SigNoz instead of a bare 500.
                    o.RecordException = true;
                })
                .AddOtlpExporter())
            .WithMetrics(metrics => metrics
                .AddAspNetCoreInstrumentation()
                .AddHttpClientInstrumentation()
                .AddRuntimeInstrumentation()
                .AddOtlpExporter());

        return builder;
    }
}
