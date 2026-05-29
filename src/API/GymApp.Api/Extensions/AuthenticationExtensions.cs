using GymApp.Common.Infrastructure.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;

namespace GymApp.Api.Extensions;

/// <summary>
/// JWT bearer authentication, including the startup fail-fast on a weak/placeholder
/// signing secret and the SignalR-over-WebSocket access-token handoff.
/// </summary>
public static class AuthenticationExtensions
{
    public static WebApplicationBuilder AddJwtAuthentication(this WebApplicationBuilder builder)
    {
        var jwtSection = builder.Configuration.GetSection("Jwt");
        builder.Services.Configure<JwtOptions>(jwtSection);

        // Fail-fast: HS256 needs ≥256 bits of entropy. Reject empty / placeholder / short keys
        // at startup so misconfigured deployments never sign tokens with a weak secret.
        {
            var jwtBoot = jwtSection.Get<JwtOptions>();
            var secret = jwtBoot?.SecretKey ?? string.Empty;
            if (string.IsNullOrWhiteSpace(secret)
                || secret.StartsWith("REPLACE_VIA_ENV", StringComparison.OrdinalIgnoreCase)
                || secret.StartsWith("CHANGE_ME", StringComparison.OrdinalIgnoreCase)
                || Encoding.UTF8.GetByteCount(secret) < 32)
            {
                throw new InvalidOperationException(
                    "Jwt:SecretKey is missing, placeholder, or shorter than 32 bytes. " +
                    "Set JWT_SECRET_KEY env (generate via: openssl rand -base64 64).");
            }
            if (string.IsNullOrWhiteSpace(jwtBoot!.Issuer) || string.IsNullOrWhiteSpace(jwtBoot.Audience))
            {
                throw new InvalidOperationException("Jwt:Issuer and Jwt:Audience must be configured.");
            }
        }

        builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options =>
            {
                var jwtOptions = jwtSection.Get<JwtOptions>()!;
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidIssuer = jwtOptions.Issuer,
                    ValidateAudience = true,
                    ValidAudience = jwtOptions.Audience,
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtOptions.SecretKey)),
                    ValidateLifetime = true,
                    ClockSkew = TimeSpan.Zero
                };
                // SignalR JS client passes the JWT via ?access_token=... on the WebSocket
                // negotiate / connect request because browsers don't permit custom headers
                // on WS handshakes. Pull it into the principal so [Authorize] hubs work.
                options.Events = new JwtBearerEvents
                {
                    OnMessageReceived = ctx =>
                    {
                        var accessToken = ctx.Request.Query["access_token"];
                        var path = ctx.HttpContext.Request.Path;
                        if (!string.IsNullOrEmpty(accessToken) && path.StartsWithSegments("/hubs"))
                        {
                            ctx.Token = accessToken;
                        }
                        return Task.CompletedTask;
                    }
                };
            });

        return builder;
    }
}
