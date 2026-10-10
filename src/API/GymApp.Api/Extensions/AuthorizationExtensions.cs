namespace GymApp.Api.Extensions;

/// <summary>
/// Authorization policies keyed off the JWT claims emitted by JwtTokenService.
/// </summary>
public static class AuthorizationExtensions
{
    public static WebApplicationBuilder AddAuthorizationPolicies(this WebApplicationBuilder builder)
    {
        builder.Services.AddAuthorizationBuilder()
            .AddPolicy("platform_admin", p => p.RequireAuthenticatedUser().RequireClaim("adminRole"))
            .AddPolicy("AdminPolicy", p => p.RequireAuthenticatedUser().RequireClaim("adminRole"))
            .AddPolicy("gym_owner", p => p.RequireAuthenticatedUser().RequireClaim("staffRole", "owner"))
            .AddPolicy("gym_staff", p => p.RequireAuthenticatedUser().RequireClaim("staffRole"))
            .AddPolicy("gym_owner_or_admin", p => p.RequireAuthenticatedUser().RequireAssertion(ctx =>
                ctx.User.HasClaim(c => c.Type == "adminRole") ||
                ctx.User.HasClaim(c => c.Type == "staffRole" && c.Value == "owner")))
            .AddPolicy("gym_owner_or_manager", p => p.RequireAuthenticatedUser().RequireAssertion(ctx =>
                // Owner principals (staffRole=owner) OR staff with gym-side sub-role of
                // Owner/Manager (carried as `staffSubRole` claim by IdentityJwtTokenService).
                ctx.User.HasClaim(c => c.Type == "staffRole" && c.Value == "owner") ||
                ctx.User.HasClaim(c =>
                    c.Type == "staffSubRole" &&
                    (string.Equals(c.Value, "Owner", StringComparison.OrdinalIgnoreCase) ||
                     string.Equals(c.Value, "Manager", StringComparison.OrdinalIgnoreCase)))))
            // Gym settings (name, contact, amenities): owner or manager per the staff
            // permission matrix in docs/01, plus platform admins who edit gyms from
            // the admin dashboard.
            .AddPolicy("gym_owner_manager_or_admin", p => p.RequireAuthenticatedUser().RequireAssertion(ctx =>
                ctx.User.HasClaim(c => c.Type == "adminRole") ||
                ctx.User.HasClaim(c => c.Type == "staffRole" && c.Value == "owner") ||
                ctx.User.HasClaim(c =>
                    c.Type == "staffSubRole" &&
                    (string.Equals(c.Value, "Owner", StringComparison.OrdinalIgnoreCase) ||
                     string.Equals(c.Value, "Manager", StringComparison.OrdinalIgnoreCase)))))
            // JwtBearer remaps the JWT "role" claim to ClaimTypes.Role on the
            // ClaimsPrincipal (MapInboundClaims = true by default), so RequireClaim
            // against the raw "role" key never matches. RequireRole reads
            // ClaimTypes.Role under the hood, which is what we want.
            .AddPolicy("trainer", p => p.RequireAuthenticatedUser().RequireRole("trainer"))
            .AddPolicy("trainee", p => p.RequireAuthenticatedUser().RequireRole("trainee"));

        return builder;
    }
}
