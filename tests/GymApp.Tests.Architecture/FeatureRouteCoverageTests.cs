using System.Text.RegularExpressions;
using FluentAssertions;
using GymApp.Common.Presentation.FeatureFlags;
using Xunit;

namespace GymApp.Tests.Architecture;

/// <summary>
/// Every versioned API route must belong to a feature flag or be explicitly always on, either via
/// endpoint metadata (<c>.RequireFeature</c> / <c>.AlwaysOn</c>) or <see cref="FeatureRouteMap"/>.
/// A new endpoint without either fails here instead of silently bypassing per-gym flags.
/// </summary>
public class FeatureRouteCoverageTests
{
    private static readonly Regex MapCall = new(
        @"Map(?<method>Get|Post|Put|Patch|Delete)\(\s*""(?<route>api/v1/[^""]*)""",
        RegexOptions.Compiled);

    [Fact]
    public void Every_api_route_is_mapped_to_a_feature_or_always_on()
    {
        var unmapped = DiscoverRoutes()
            .Where(r => !r.HasExplicitMetadata && FeatureRouteMap.Resolve(new RouteRequest(r.Method, r.Route)) is null)
            .Select(r => $"{r.Method} {r.Route} ({r.File})")
            .ToList();

        unmapped.Should().BeEmpty("each route must be added to FeatureRouteMap or tagged .RequireFeature/.AlwaysOn");
    }

    [Fact]
    public void Route_discovery_finds_the_api()
    {
        DiscoverRoutes().Count.Should().BeGreaterThan(150);
    }

    private sealed record DiscoveredRoute(string Method, string Route, string File, bool HasExplicitMetadata);

    private static List<DiscoveredRoute> DiscoverRoutes()
    {
        var srcDir = Path.Combine(FindRepoRoot(), "src");
        var routes = new List<DiscoveredRoute>();

        foreach (var file in Directory.EnumerateFiles(srcDir, "*.cs", SearchOption.AllDirectories))
        {
            if (file.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}"))
                continue;

            var text = File.ReadAllText(file);
            var explicitTag = text.Contains(".RequireFeature(") || text.Contains(".AlwaysOn()");
            foreach (Match m in MapCall.Matches(text))
            {
                routes.Add(new DiscoveredRoute(
                    m.Groups["method"].Value.ToUpperInvariant(),
                    m.Groups["route"].Value,
                    Path.GetFileName(file),
                    explicitTag));
            }
        }

        return routes;
    }

    private static string FindRepoRoot()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null && !File.Exists(Path.Combine(dir.FullName, "GymApp.sln")))
            dir = dir.Parent;

        return dir?.FullName ?? throw new InvalidOperationException("GymApp.sln not found above the test output folder.");
    }
}
