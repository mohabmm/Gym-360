using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using GymApp.Modules.Identity.Shared;
using GymApp.Tests.Integration.Abstractions;

namespace GymApp.Tests.Integration.Gyms;

/// <summary>
/// Guards the member-search contract added for the staff roster: the search runs
/// server-side via <see cref="IUserProfileLookup.FilterProfileIdsAsync"/> against the
/// real SqlServer provider. Email/PhoneNumber are value-converted (HasConversion), so
/// the query must PROJECT the rows (where `.Value` translates) and match in memory —
/// a `.Value` LIKE in a WHERE clause would throw "could not be translated" at runtime.
/// These tests fail loudly if that translation regresses.
/// </summary>
public sealed class MemberSearchIntegrationTests : BaseIntegrationTest
{
    public MemberSearchIntegrationTests(IntegrationTestWebAppFactory factory)
        : base(factory) { }

    private async Task<Guid> RegisterUserAsync(string firstName, string lastName, string email, string phone)
    {
        var response = await HttpClient.PostAsJsonAsync("api/v1/identity/register", new
        {
            Email = email,
            Password = "StrongP@ss123!",
            FirstName = firstName,
            LastName = lastName,
            Phone = phone,
            Role = "Trainee",
        });
        response.StatusCode.Should().Be(HttpStatusCode.Created);

        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        var accessToken = body.GetProperty("data").GetProperty("accessToken").GetString()!;
        return Guid.Parse(SubjectFromJwt(accessToken));
    }

    private static string SubjectFromJwt(string jwt)
    {
        var payload = jwt.Split('.')[1];
        var padded = payload.Replace('-', '+').Replace('_', '/');
        padded = padded.PadRight(padded.Length + (4 - padded.Length % 4) % 4, '=');
        using var doc = JsonDocument.Parse(Encoding.UTF8.GetString(Convert.FromBase64String(padded)));
        var root = doc.RootElement;
        foreach (var name in new[] { "sub", "nameid", "userId", "id" })
        {
            if (root.TryGetProperty(name, out var v) && v.ValueKind == JsonValueKind.String)
                return v.GetString()!;
        }
        // Common ASP.NET claim URI for the subject.
        const string nameIdentifier = "http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier";
        if (root.TryGetProperty(nameIdentifier, out var nid))
            return nid.GetString()!;
        throw new InvalidOperationException("No subject claim in JWT.");
    }

    [Fact]
    public async Task FilterProfileIds_MatchesByNameEmailPhone_AndTranslatesToSql()
    {
        // Arrange — a uniquely identifiable user committed through the real flow.
        var unique = Guid.NewGuid().ToString("N")[..8];
        var firstName = $"Zenith{unique}";
        var email = $"{unique}@search.test";
        var phone = $"+20100{Random.Shared.Next(1000000, 9999999)}";
        var userId = await RegisterUserAsync(firstName, "Roster", email, phone);

        var lookup = GetService<IUserProfileLookup>();
        var candidates = new[] { userId };

        // Each call EXECUTES the EF query — a translation regression throws here.
        (await lookup.FilterProfileIdsAsync(candidates, firstName, null))
            .Should().Contain(userId, "search should match by first name");

        (await lookup.FilterProfileIdsAsync(candidates, unique, null))
            .Should().Contain(userId, "search should match an email fragment");

        (await lookup.FilterProfileIdsAsync(candidates, phone[4..], null))
            .Should().Contain(userId, "search should match a phone fragment");

        (await lookup.FilterProfileIdsAsync(candidates, "zzz-no-such-member-zzz", null))
            .Should().NotContain(userId, "a non-matching term excludes the user");
    }

    [Fact]
    public async Task FilterProfileIds_StatusPredicate_TranslatesToSql()
    {
        var unique = Guid.NewGuid().ToString("N")[..8];
        var userId = await RegisterUserAsync($"Status{unique}", "Roster", $"{unique}@status.test",
            $"+20101{Random.Shared.Next(1000000, 9999999)}");
        var candidates = new[] { userId };

        // Status equality is a value-converted-enum predicate — assert it runs (no throw)
        // and returns a well-formed result regardless of the freshly-registered status.
        var active = await lookup_StatusCall(candidates, "active");
        active.Should().NotBeNull();

        var inactive = await lookup_StatusCall(candidates, "inactive");
        inactive.Should().NotBeNull();

        // Combined search + status must also translate.
        var combined = await GetService<IUserProfileLookup>()
            .FilterProfileIdsAsync(candidates, unique, "active");
        combined.Should().NotBeNull();
    }

    private Task<IReadOnlyList<Guid>> lookup_StatusCall(Guid[] candidates, string status)
        => GetService<IUserProfileLookup>().FilterProfileIdsAsync(candidates, null, status);
}
