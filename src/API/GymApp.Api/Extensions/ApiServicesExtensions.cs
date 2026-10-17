using System.Threading.RateLimiting;
using GymApp.Api.Middleware;
using GymApp.Common.Infrastructure.Outbox;
using Microsoft.AspNetCore.RateLimiting;

namespace GymApp.Api.Extensions;

/// <summary>
/// API surface services: exception handling, JSON serialization, Swagger, SignalR,
/// health checks and CORS.
/// </summary>
public static class ApiServicesExtensions
{
    public static WebApplicationBuilder AddApiServices(this WebApplicationBuilder builder)
    {
        // --- Exception handler ---
        builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
        builder.Services.AddProblemDetails();

        // --- JSON serialization: camelCase properties, case-insensitive enum parsing ---
        builder.Services.ConfigureHttpJsonOptions(options =>
        {
            options.SerializerOptions.PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase;
            options.SerializerOptions.Converters.Add(
                new System.Text.Json.Serialization.JsonStringEnumConverter(System.Text.Json.JsonNamingPolicy.CamelCase));
        });

        AddSwagger(builder.Services);
        AddClientLogsRateLimiter(builder.Services);

        // --- SignalR ---
        builder.Services.AddSignalR();
        builder.Services.AddScoped<GymApp.Common.Application.Abstractions.IChatNotifier, GymApp.Api.Hubs.SignalRChatNotifier>();

        // --- Health Checks ---
        builder.Services.AddHealthChecks()
            .AddDbContextCheck<OutboxDbContext>("outbox-db", tags: new[] { "ready" })
            .AddRedis(
                builder.Configuration.GetConnectionString("Redis") ?? "localhost:6379",
                name: "redis",
                tags: new[] { "ready" });

        AddCorsPolicy(builder);

        return builder;
    }

    private static void AddSwagger(IServiceCollection services)
    {
        // --- Swagger ---
        services.AddEndpointsApiExplorer();
        services.AddSwaggerGen(options =>
        {
            options.SwaggerDoc("v1", new() { Title = "GymApp API", Version = "v1" });
            // Many endpoints declare nested "Request" records (e.g. AddMemberEndpoint.Request).
            // The default schemaId selector uses the short name and collides across modules.
            options.CustomSchemaIds(t => t.FullName!.Replace('+', '.'));
            // Emit enums as their camelCase string names to match the JsonStringEnumConverter
            // registered on ConfigureHttpJsonOptions — otherwise clients see "enum":[0,1,2]
            // while the server expects "flagOnly"/"notifyTrainer"/"suspendBooking".
            options.UseAllOfToExtendReferenceSchemas();
            options.SchemaFilter<StringEnumSchemaFilter>();
            options.AddSecurityDefinition("Bearer", new Microsoft.OpenApi.Models.OpenApiSecurityScheme
            {
                Name = "Authorization",
                Type = Microsoft.OpenApi.Models.SecuritySchemeType.Http,
                Scheme = "bearer",
                BearerFormat = "JWT",
                In = Microsoft.OpenApi.Models.ParameterLocation.Header,
            });
            options.AddSecurityRequirement(new Microsoft.OpenApi.Models.OpenApiSecurityRequirement
            {
                {
                    new Microsoft.OpenApi.Models.OpenApiSecurityScheme
                    {
                        Reference = new Microsoft.OpenApi.Models.OpenApiReference
                        {
                            Type = Microsoft.OpenApi.Models.ReferenceType.SecurityScheme,
                            Id = "Bearer"
                        }
                    },
                    Array.Empty<string>()
                }
            });
        });
    }

    private static void AddClientLogsRateLimiter(IServiceCollection services)
    {
        // --- Rate limiting ---
        // Used by the client-telemetry ingest endpoint so one crash-looping device
        // can't flood log ingest. Partitioned by authenticated user, else client IP.
        services.AddRateLimiter(options =>
        {
            options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
            options.AddPolicy("client-logs", httpContext =>
            {
                var partitionKey = httpContext.User.FindFirst(
                        System.IdentityModel.Tokens.Jwt.JwtRegisteredClaimNames.Sub)?.Value
                    ?? httpContext.Connection.RemoteIpAddress?.ToString()
                    ?? "anonymous";
                return RateLimitPartition.GetFixedWindowLimiter(partitionKey, _ =>
                    new FixedWindowRateLimiterOptions
                    {
                        // Each batch can carry up to 100 records; 60 batches/min/device
                        // is generous for telemetry yet caps a runaway client.
                        PermitLimit = 60,
                        Window = TimeSpan.FromMinutes(1),
                        QueueLimit = 0,
                    });
            });
        });
    }

    private static void AddCorsPolicy(WebApplicationBuilder builder)
    {
        // --- CORS ---
        // Origins are read from Cors:AllowedOrigins. We NEVER fall back to AllowAnyOrigin:
        // if the configured list is empty we warn and use a localhost-only fallback so
        // misconfiguration fails closed rather than open.
        var configuredOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>()
            ?? Array.Empty<string>();

        string[] allowedOrigins;
        if (configuredOrigins.Length == 0)
        {
            allowedOrigins = new[] { "http://localhost:3000", "http://localhost:3001" };
            Console.Error.WriteLine(
                $"[WARN] CORS:AllowedOrigins is empty. Falling back to localhost-only origins: " +
                $"{string.Join(", ", allowedOrigins)}. Configure Cors:AllowedOrigins in appsettings " +
                $"or the CORS_ORIGIN_* env vars for production.");
        }
        else
        {
            allowedOrigins = configuredOrigins;
        }

        builder.Services.AddCors(options =>
        {
            options.AddDefaultPolicy(policy =>
            {
                policy.WithOrigins(allowedOrigins)
                      .AllowAnyMethod()
                      .AllowAnyHeader()
                      .AllowCredentials(); // needed for SignalR WebSocket
            });
        });
    }
}
