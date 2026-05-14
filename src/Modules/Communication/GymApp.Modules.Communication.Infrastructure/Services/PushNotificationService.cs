using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using GymApp.Modules.Communication.Application.Contracts;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

namespace GymApp.Modules.Communication.Infrastructure.Services;

internal sealed class PushNotificationService : IPushNotificationService
{
    private const string FcmSendUrlTemplate = "https://fcm.googleapis.com/v1/projects/{0}/messages:send";
    private const string GoogleTokenUrl = "https://oauth2.googleapis.com/token";
    private const string FcmScope = "https://www.googleapis.com/auth/firebase.messaging";

    private readonly IFcmTokenStore _tokenStore;
    private readonly IConfiguration _configuration;
    private readonly HttpClient _httpClient;
    private readonly ILogger<PushNotificationService> _logger;

    // Cached OAuth2 access token
    private string? _cachedAccessToken;
    private DateTime _tokenExpiresAt = DateTime.MinValue;
    private readonly SemaphoreSlim _tokenLock = new(1, 1);

    public PushNotificationService(
        IFcmTokenStore tokenStore,
        IConfiguration configuration,
        HttpClient httpClient,
        ILogger<PushNotificationService> logger)
    {
        _tokenStore = tokenStore;
        _configuration = configuration;
        _httpClient = httpClient;
        _logger = logger;
    }

    public async Task SendAsync(
        Guid userId,
        string title,
        string body,
        Dictionary<string, string>? data = null,
        CancellationToken cancellationToken = default)
    {
        bool isEnabled = _configuration.GetValue<bool>("Fcm:IsEnabled");

        if (!isEnabled)
        {
            _logger.LogInformation(
                "FCM disabled (dev mode). UserId={UserId}, Title={Title}, Body={Body}, Data={Data}",
                userId,
                title,
                body,
                data is not null ? JsonSerializer.Serialize(data) : "null");
            return;
        }

        var tokens = await _tokenStore.GetTokensForUserAsync(userId, cancellationToken);

        if (tokens.Count == 0)
        {
            _logger.LogDebug("No FCM tokens registered for UserId={UserId}", userId);
            return;
        }

        string projectId = _configuration["Fcm:ProjectId"] ?? throw new InvalidOperationException("Fcm:ProjectId is not configured.");
        string sendUrl = string.Format(FcmSendUrlTemplate, projectId);

        string accessToken = await GetAccessTokenAsync(cancellationToken);

        var tasks = tokens.Select(token => SendToTokenAsync(sendUrl, accessToken, token, title, body, data, cancellationToken));
        await Task.WhenAll(tasks);
    }

    private async Task SendToTokenAsync(
        string sendUrl,
        string accessToken,
        string deviceToken,
        string title,
        string body,
        Dictionary<string, string>? data,
        CancellationToken cancellationToken)
    {
        var message = new
        {
            message = new
            {
                token = deviceToken,
                notification = new { title, body },
                data = data ?? new Dictionary<string, string>(),
                android = new { priority = "high" },
                apns = new { headers = new { apns_priority = "10" } }
            }
        };

        var json = JsonSerializer.Serialize(message, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower
        });

        using var request = new HttpRequestMessage(HttpMethod.Post, sendUrl);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        request.Content = new StringContent(json, Encoding.UTF8, "application/json");

        using var response = await _httpClient.SendAsync(request, cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            var errorBody = await response.Content.ReadAsStringAsync(cancellationToken);
            _logger.LogWarning(
                "FCM send failed for token {Token}: {StatusCode} {Error}",
                deviceToken[..Math.Min(20, deviceToken.Length)],
                response.StatusCode,
                errorBody);
        }
        else
        {
            _logger.LogDebug("FCM notification sent successfully to token {TokenPrefix}...", deviceToken[..Math.Min(20, deviceToken.Length)]);
        }
    }

    private async Task<string> GetAccessTokenAsync(CancellationToken cancellationToken)
    {
        // Fast path — return cached token if still valid (with 60s buffer)
        if (_cachedAccessToken is not null && DateTime.UtcNow < _tokenExpiresAt.AddSeconds(-60))
            return _cachedAccessToken;

        await _tokenLock.WaitAsync(cancellationToken);
        try
        {
            // Double-check after acquiring lock
            if (_cachedAccessToken is not null && DateTime.UtcNow < _tokenExpiresAt.AddSeconds(-60))
                return _cachedAccessToken;

            var (newToken, expiresIn) = await FetchGoogleAccessTokenAsync(cancellationToken);
            _cachedAccessToken = newToken;
            _tokenExpiresAt = DateTime.UtcNow.AddSeconds(expiresIn);
            return _cachedAccessToken;
        }
        finally
        {
            _tokenLock.Release();
        }
    }

    private async Task<(string Token, int ExpiresIn)> FetchGoogleAccessTokenAsync(CancellationToken cancellationToken)
    {
        var serviceAccount = LoadServiceAccount();

        string jwt = CreateServiceAccountJwt(serviceAccount);

        var requestBody = new FormUrlEncodedContent(new[]
        {
            new KeyValuePair<string, string>("grant_type", "urn:ietf:params:oauth:grant-type:jwt-bearer"),
            new KeyValuePair<string, string>("assertion", jwt)
        });

        using var response = await _httpClient.PostAsync(GoogleTokenUrl, requestBody, cancellationToken);
        response.EnsureSuccessStatusCode();

        var responseJson = await response.Content.ReadAsStringAsync(cancellationToken);
        var tokenResponse = JsonSerializer.Deserialize<GoogleTokenResponse>(responseJson)
            ?? throw new InvalidOperationException("Failed to deserialize Google token response.");

        return (tokenResponse.AccessToken, tokenResponse.ExpiresIn);
    }

    private string CreateServiceAccountJwt(ServiceAccountCredentials credentials)
    {
        var now = DateTimeOffset.UtcNow;
        var expiry = now.AddMinutes(60);

        // Strip PEM headers and decode the base64 private key
        var privateKeyPem = credentials.PrivateKey
            .Replace("-----BEGIN PRIVATE KEY-----", string.Empty)
            .Replace("-----END PRIVATE KEY-----", string.Empty)
            .Replace("-----BEGIN RSA PRIVATE KEY-----", string.Empty)
            .Replace("-----END RSA PRIVATE KEY-----", string.Empty)
            .Replace("\n", string.Empty)
            .Replace("\r", string.Empty)
            .Trim();

        var privateKeyBytes = Convert.FromBase64String(privateKeyPem);

        using var rsa = RSA.Create();

        // Try PKCS#8 first (standard Google service account format), fall back to PKCS#1
        try
        {
            rsa.ImportPkcs8PrivateKey(privateKeyBytes, out _);
        }
        catch
        {
            rsa.ImportRSAPrivateKey(privateKeyBytes, out _);
        }

        var rsaSecurityKey = new RsaSecurityKey(rsa.ExportParameters(true))
        {
            KeyId = credentials.PrivateKeyId
        };

        var signingCredentials = new SigningCredentials(rsaSecurityKey, SecurityAlgorithms.RsaSha256);

        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Iss, credentials.ClientEmail),
            new Claim(JwtRegisteredClaimNames.Sub, credentials.ClientEmail),
            new Claim(JwtRegisteredClaimNames.Aud, GoogleTokenUrl),
            new Claim("scope", FcmScope),
            new Claim(JwtRegisteredClaimNames.Iat, now.ToUnixTimeSeconds().ToString(), ClaimValueTypes.Integer64),
            new Claim(JwtRegisteredClaimNames.Exp, expiry.ToUnixTimeSeconds().ToString(), ClaimValueTypes.Integer64)
        };

        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(claims),
            SigningCredentials = signingCredentials
        };

        var tokenHandler = new JwtSecurityTokenHandler();
        var token = tokenHandler.CreateToken(tokenDescriptor);
        return tokenHandler.WriteToken(token);
    }

    private ServiceAccountCredentials LoadServiceAccount()
    {
        var keyPath = _configuration["Fcm:ServiceAccountKeyPath"];
        var keyJson = _configuration["Fcm:ServiceAccountJson"];

        string json;
        if (!string.IsNullOrWhiteSpace(keyJson))
        {
            json = keyJson;
        }
        else if (!string.IsNullOrWhiteSpace(keyPath))
        {
            if (!File.Exists(keyPath))
                throw new FileNotFoundException($"FCM service account key file not found: {keyPath}");
            json = File.ReadAllText(keyPath);
        }
        else
        {
            throw new InvalidOperationException(
                "FCM service account credentials are not configured. Set 'Fcm:ServiceAccountKeyPath' or 'Fcm:ServiceAccountJson'.");
        }

        return JsonSerializer.Deserialize<ServiceAccountCredentials>(json)
            ?? throw new InvalidOperationException("Failed to deserialize FCM service account JSON.");
    }

    private sealed class ServiceAccountCredentials
    {
        [JsonPropertyName("type")]
        public string Type { get; set; } = default!;

        [JsonPropertyName("project_id")]
        public string ProjectId { get; set; } = default!;

        [JsonPropertyName("private_key_id")]
        public string PrivateKeyId { get; set; } = default!;

        [JsonPropertyName("private_key")]
        public string PrivateKey { get; set; } = default!;

        [JsonPropertyName("client_email")]
        public string ClientEmail { get; set; } = default!;

        [JsonPropertyName("client_id")]
        public string ClientId { get; set; } = default!;

        [JsonPropertyName("auth_uri")]
        public string AuthUri { get; set; } = default!;

        [JsonPropertyName("token_uri")]
        public string TokenUri { get; set; } = default!;
    }

    private sealed class GoogleTokenResponse
    {
        [JsonPropertyName("access_token")]
        public string AccessToken { get; set; } = default!;

        [JsonPropertyName("expires_in")]
        public int ExpiresIn { get; set; }

        [JsonPropertyName("token_type")]
        public string TokenType { get; set; } = default!;
    }
}
