using System.Linq;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using GymApp.Tests.Integration.Abstractions;
using Xunit;

namespace GymApp.Tests.Integration.Observability;

/// <summary>
/// Covers the mobile telemetry ingest endpoint (POST /api/v1/observability/client-logs).
/// The endpoint is <c>AllowAnonymous</c> (login-screen errors have no JWT yet) and
/// re-emits each record through the server logging pipeline, returning the count it
/// accepted. These tests pin its contract: anonymous access, the per-request record
/// cap, and that PII-bearing payloads are accepted without throwing.
/// </summary>
public sealed class ClientLogsIntegrationTests : BaseIntegrationTest
{
    private const string Endpoint = "api/v1/observability/client-logs";

    public ClientLogsIntegrationTests(IntegrationTestWebAppFactory factory)
        : base(factory) { }

    private static object Record(string level = "error", string message = "boom") => new
    {
        level,
        message,
        app = "gymapp-trainer",
        appVersion = "1.0.0+1",
        os = "ios",
        osVersion = "17.0",
        device = "iPhone15,2",
        route = "/availability",
        errorType = "DioException",
        clientTs = "2026-05-31T12:00:00Z",
    };

    private static async Task<int> AcceptedAsync(HttpResponseMessage response)
    {
        var json = await response.Content.ReadFromJsonAsync<JsonElement>();
        return json.GetProperty("data").GetProperty("accepted").GetInt32();
    }

    [Fact]
    public async Task Post_Anonymous_IsAccepted()
    {
        // No Authorization header — endpoint is AllowAnonymous so this must succeed.
        var response = await HttpClient.PostAsJsonAsync(
            Endpoint, new { records = new[] { Record() } });

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        (await AcceptedAsync(response)).Should().Be(1);
    }

    [Fact]
    public async Task Post_Authenticated_AcceptsWholeBatch()
    {
        var client = CreateAuthenticatedClient(role: "trainee");

        var response = await client.PostAsJsonAsync(
            Endpoint, new { records = new[] { Record(), Record(level: "warning", message: "API 400") } });

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        (await AcceptedAsync(response)).Should().Be(2);
    }

    [Fact]
    public async Task Post_EmptyBatch_AcceptsZero()
    {
        var response = await HttpClient.PostAsJsonAsync(
            Endpoint, new { records = Array.Empty<object>() });

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        (await AcceptedAsync(response)).Should().Be(0);
    }

    [Fact]
    public async Task Post_OversizedBatch_IsCappedAt100()
    {
        var records = Enumerable.Range(0, 150).Select(i => Record(message: $"e{i}")).ToArray();

        var response = await HttpClient.PostAsJsonAsync(Endpoint, new { records });

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        // Server keeps at most MaxRecordsPerRequest (100); the rest are dropped.
        (await AcceptedAsync(response)).Should().Be(100);
    }

    [Fact]
    public async Task Post_PayloadWithPii_IsAcceptedAndScrubbed()
    {
        // Blocked attribute keys + free-text PII must not throw; the record is still
        // accepted (scrubbing happens server-side before the log is emitted).
        var record = new
        {
            level = "error",
            message = "login failed for user@example.com token eyJhbGciOiJIUzI1NiJ9.abcdef",
            app = "gymapp-trainer",
            route = "/login",
            attributes = new Dictionary<string, object>
            {
                ["password"] = "hunter2",
                ["token"] = "secret-token",
                ["email"] = "user@example.com",
                ["http.status"] = 400,
            },
        };

        var response = await HttpClient.PostAsJsonAsync(Endpoint, new { records = new[] { record } });

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        (await AcceptedAsync(response)).Should().Be(1);
    }
}
