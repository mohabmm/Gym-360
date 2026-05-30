using System.Diagnostics;
using OpenTelemetry;
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
///   OTEL_EXPORTER_OTLP_ENDPOINT=http://signoz-otel-collector:4318
///   OTEL_EXPORTER_OTLP_PROTOCOL=http/protobuf  (recommended over the Docker network; "grpc" also supported on :4317)
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
                // SqlClient instrumentation 1.14+ emits ONLY the *stable* DB semantic
                // conventions (db.system.name / db.namespace / db.query.text). Our
                // SigNoz "DB Call Metrics" tab keys off the *old* experimental
                // attribute db.system, so it reads empty. The migration env var
                // (OTEL_SEMCONV_STABILITY_OPT_IN=database/dup) was dropped in the
                // stable release, so we back-fill the legacy attribute names on each
                // DB span ourselves. Cheap, runs only when OTel is enabled.
                .AddProcessor(new SignozDbSemconvCompatProcessor())
                .AddOtlpExporter())
            .WithMetrics(metrics => metrics
                .AddAspNetCoreInstrumentation()
                .AddHttpClientInstrumentation()
                .AddRuntimeInstrumentation()
                .AddOtlpExporter());

        return builder;
    }
}

/// <summary>
/// Back-fills the legacy (experimental) DB span attributes from the stable v1.33
/// conventions emitted by OpenTelemetry.Instrumentation.SqlClient 1.14+, so the
/// self-hosted SigNoz "DB Call Metrics" tab (which still keys off <c>db.system</c>)
/// recognises our SQL Server spans. Runs on span end, only for spans that already
/// carry a stable DB attribute — non-DB spans are untouched.
/// </summary>
internal sealed class SignozDbSemconvCompatProcessor : BaseProcessor<Activity>
{
    public override void OnEnd(Activity activity)
    {
        if (activity.GetTagItem("db.system.name") is not string system)
            return; // not a DB span under the stable conventions — leave it alone.

        // SigNoz groups DB Call Metrics by the legacy db.system value ("mssql").
        // The stable convention reports "microsoft.sql_server"; normalise it.
        if (activity.GetTagItem("db.system") is null)
            activity.SetTag("db.system", system == "microsoft.sql_server" ? "mssql" : system);

        if (activity.GetTagItem("db.name") is null && activity.GetTagItem("db.namespace") is { } ns)
            activity.SetTag("db.name", ns);

        if (activity.GetTagItem("db.statement") is null && activity.GetTagItem("db.query.text") is { } text)
            activity.SetTag("db.statement", text);

        if (activity.GetTagItem("db.operation") is null && activity.GetTagItem("db.operation.name") is { } op)
            activity.SetTag("db.operation", op);
    }
}
