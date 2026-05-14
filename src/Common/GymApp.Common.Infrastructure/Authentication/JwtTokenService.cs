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

public interface IJwtTokenService
{
    string GenerateAccessToken(
        Guid userId,
        string email,
        string role,
        Guid? gymId = null,
        string? staffRole = null,
        string? adminRole = null,
        string? principalType = null,
        Guid? ownerId = null,
        string? schemaPrefix = null,
        string? staffSubRole = null);

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

    public string GenerateAccessToken(
        Guid userId,
        string email,
        string role,
        Guid? gymId = null,
        string? staffRole = null,
        string? adminRole = null,
        string? principalType = null,
        Guid? ownerId = null,
        string? schemaPrefix = null,
        string? staffSubRole = null)
    {
        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, userId.ToString()),
            new(JwtRegisteredClaimNames.Email, email),
            new("role", role),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        };

        if (gymId.HasValue)
            claims.Add(new Claim("gymId", gymId.Value.ToString()));

        if (!string.IsNullOrEmpty(staffRole))
            claims.Add(new Claim("staffRole", staffRole));

        if (!string.IsNullOrEmpty(adminRole))
            claims.Add(new Claim("adminRole", adminRole));

        if (!string.IsNullOrEmpty(principalType))
            claims.Add(new Claim("principalType", principalType));

        if (ownerId.HasValue)
            claims.Add(new Claim("ownerId", ownerId.Value.ToString()));

        if (!string.IsNullOrEmpty(schemaPrefix))
            claims.Add(new Claim("schemaPrefix", schemaPrefix));

        if (!string.IsNullOrEmpty(staffSubRole))
            claims.Add(new Claim("staffSubRole", staffSubRole));

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_options.SecretKey));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: _options.Issuer,
            audience: _options.Audience,
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(_options.AccessTokenExpirationMinutes),
            signingCredentials: credentials);

        return new JwtSecurityTokenHandler().WriteToken(token);
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
