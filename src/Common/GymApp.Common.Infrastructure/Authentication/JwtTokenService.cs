using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace GymApp.Common.Infrastructure.Authentication;

public sealed class JwtOptions
{
    public string Issuer { get; init; } = string.Empty;
    public string Audience { get; init; } = string.Empty;
    public string SecretKey { get; init; } = string.Empty;
    public int AccessTokenExpirationMinutes { get; init; } = 60;
    public int RefreshTokenExpirationDays { get; init; } = 30;
}

/// <summary>
/// The identity and scope claims carried by an access token. Optional claims are
/// omitted from the token when null/empty.
/// </summary>
public sealed record AccessTokenClaims(Guid UserId, string Email, string Role)
{
    public Guid? GymId { get; init; }
    public string? StaffRole { get; init; }
    public string? AdminRole { get; init; }
    public string? PrincipalType { get; init; }
    public Guid? OwnerId { get; init; }
    public string? SchemaPrefix { get; init; }
    public string? StaffSubRole { get; init; }
}

public interface IJwtTokenService
{
    string GenerateAccessToken(AccessTokenClaims claims);

    string GenerateRefreshToken();
    ClaimsPrincipal? ValidateToken(string token);
}

public sealed class JwtTokenService : IJwtTokenService
{
    private readonly JwtOptions _options;

    public JwtTokenService(IOptions<JwtOptions> options)
    {
        _options = options.Value;
    }

    public string GenerateAccessToken(AccessTokenClaims claims)
    {
        var tokenClaims = BuildClaims(claims);

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_options.SecretKey));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: _options.Issuer,
            audience: _options.Audience,
            claims: tokenClaims,
            expires: DateTime.UtcNow.AddMinutes(_options.AccessTokenExpirationMinutes),
            signingCredentials: credentials);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    private static List<Claim> BuildClaims(AccessTokenClaims claims)
    {
        var tokenClaims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, claims.UserId.ToString()),
            new(JwtRegisteredClaimNames.Email, claims.Email),
            new("role", claims.Role),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        };

        AddIfPresent(tokenClaims, "gymId", claims.GymId?.ToString());
        AddIfPresent(tokenClaims, "staffRole", claims.StaffRole);
        AddIfPresent(tokenClaims, "adminRole", claims.AdminRole);
        AddIfPresent(tokenClaims, "principalType", claims.PrincipalType);
        AddIfPresent(tokenClaims, "ownerId", claims.OwnerId?.ToString());
        AddIfPresent(tokenClaims, "schemaPrefix", claims.SchemaPrefix);
        AddIfPresent(tokenClaims, "staffSubRole", claims.StaffSubRole);

        return tokenClaims;
    }

    private static void AddIfPresent(List<Claim> claims, string type, string? value)
    {
        if (!string.IsNullOrEmpty(value))
            claims.Add(new Claim(type, value));
    }

    public string GenerateRefreshToken()
    {
        // Cryptographically random 64 bytes -> 88-char base64 string.
        // Much stronger than Guid.NewGuid (which is not for secrets).
        return Convert.ToBase64String(RandomNumberGenerator.GetBytes(64));
    }

    public ClaimsPrincipal? ValidateToken(string token)
    {
        var tokenHandler = new JwtSecurityTokenHandler();
        var key = Encoding.UTF8.GetBytes(_options.SecretKey);

        try
        {
            return tokenHandler.ValidateToken(token, new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidIssuer = _options.Issuer,
                ValidateAudience = true,
                ValidAudience = _options.Audience,
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(key),
                ValidateLifetime = true,
                ClockSkew = TimeSpan.Zero
            }, out _);
        }
        catch
        {
            return null;
        }
    }
}
