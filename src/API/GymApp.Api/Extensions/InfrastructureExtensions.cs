using GymApp.Common.Application.Abstractions;
using GymApp.Common.Application.Behaviors;
using GymApp.Common.Application.Messaging;
using GymApp.Common.Infrastructure.Authentication;
using GymApp.Common.Infrastructure.Caching;
using GymApp.Common.Infrastructure.Clock;
using GymApp.Common.Infrastructure.Interceptors;
using GymApp.Common.Infrastructure.Messaging;
using GymApp.Common.Infrastructure.Outbox;
using GymApp.Common.Infrastructure.Tenancy;
using Microsoft.EntityFrameworkCore;
using Quartz;
using Serilog;
using StackExchange.Redis;

namespace GymApp.Api.Extensions;

/// <summary>
/// Cross-cutting platform infrastructure: logging, clock, JWT service, the
/// integration-event outbox plumbing, tenancy, Redis, the outbox DbContext +
/// Quartz processor, and MediatR with its pipeline behaviors.
/// </summary>
public static class InfrastructureExtensions
{
    public static WebApplicationBuilder AddCommonInfrastructure(this WebApplicationBuilder builder)
    {
        // --- Serilog ---
        // Sinks come from appsettings "Serilog" (Console + Seq). When an OTLP
        // endpoint is configured (Dokploy/SigNoz), ALSO ship logs over OTLP so
        // logs correlate with traces in SigNoz. No endpoint set → Seq-only, as before.
        builder.Host.UseSerilog((context, configuration) =>
        {
            configuration.ReadFrom.Configuration(context.Configuration);

            var otlpEndpoint = context.Configuration["OTEL_EXPORTER_OTLP_ENDPOINT"]
                               ?? context.Configuration["Otel:Endpoint"];
            if (!string.IsNullOrWhiteSpace(otlpEndpoint))
            {
                var serviceName = context.Configuration["OTEL_SERVICE_NAME"]
                                  ?? context.Configuration["Otel:ServiceName"]
                                  ?? "gymapp-api";
                var useHttp = string.Equals(
                    context.Configuration["OTEL_EXPORTER_OTLP_PROTOCOL"],
                    "http/protobuf",
                    StringComparison.OrdinalIgnoreCase);

                configuration.WriteTo.OpenTelemetry(options =>
                {
                    // Serilog's OTLP sink wants the signal-specific logs path for HTTP,
                    // and the base gRPC endpoint otherwise.
                    options.Endpoint = useHttp ? $"{otlpEndpoint.TrimEnd('/')}/v1/logs" : otlpEndpoint;
                    options.Protocol = useHttp
                        ? Serilog.Sinks.OpenTelemetry.OtlpProtocol.HttpProtobuf
                        : Serilog.Sinks.OpenTelemetry.OtlpProtocol.Grpc;
                    options.ResourceAttributes = new Dictionary<string, object>
                    {
                        ["service.name"] = serviceName,
                    };
                });
            }
        });

        // --- Common services ---
        builder.Services.AddSingleton<IDateTimeProvider, DateTimeProvider>();
        builder.Services.AddSingleton<IJwtTokenService, JwtTokenService>();

        // Integration-event outbox plumbing. The queue + interceptor are SCOPED so the interceptor
        // (resolved per-scope inside each module's AddDbContext lambda) shares the same queue instance
        // that IIntegrationEventPublisher writes to during the command.
        builder.Services.AddScoped<IntegrationEventQueue>();
        builder.Services.AddScoped<IIntegrationEventPublisher, IntegrationEventPublisher>();
        builder.Services.AddScoped<ConvertDomainEventsToOutboxMessagesInterceptor>();

        // --- Tenancy ---
        // Default mode = Soft (legacy schema names). Switch to Hard for per-Gym isolation.
        builder.Services.AddTenancy(builder.Configuration);

        // --- Redis ---
        var redisConnection = builder.Configuration.GetConnectionString("Redis") ?? "localhost:6379";
        builder.Services.AddSingleton<IConnectionMultiplexer>(_ => ConnectionMultiplexer.Connect(redisConnection));
        builder.Services.AddSingleton<ICacheService, RedisCacheService>();

        // --- Outbox DbContext ---
        builder.Services.AddDbContext<OutboxDbContext>(options =>
            options.UseSqlServer(builder.Configuration.GetConnectionString("Database"))
                .UseTenantSchemaResolution());

        builder.Services.Configure<OutboxOptions>(builder.Configuration.GetSection("Outbox"));

        // --- Quartz (Outbox processor) ---
        builder.Services.AddQuartz(q =>
        {
            var jobKey = new JobKey(nameof(ProcessOutboxMessagesJob));
            q.AddJob<ProcessOutboxMessagesJob>(opts => opts.WithIdentity(jobKey));
            q.AddTrigger(opts => opts
                .ForJob(jobKey)
                .WithIdentity($"{nameof(ProcessOutboxMessagesJob)}-trigger")
                .WithSimpleSchedule(schedule => schedule
                    .WithIntervalInSeconds(10)
                    .RepeatForever()));
        });
        builder.Services.AddQuartzHostedService();

        // --- MediatR (API assembly only — each module registers its own handlers via Add{Module}Module) ---
        // Pipeline behaviors run for every request across every module.
        builder.Services.AddMediatR(cfg =>
        {
            cfg.RegisterServicesFromAssembly(typeof(Program).Assembly);
            cfg.AddOpenBehavior(typeof(LoggingBehavior<,>));
            cfg.AddOpenBehavior(typeof(ValidationBehavior<,>));
        });

        return builder;
    }
}
