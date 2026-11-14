using GymApp.Common.Application.FeatureFlags;
using K = GymApp.Common.Application.FeatureFlags.FeatureKeys;

namespace GymApp.Common.Presentation.FeatureFlags;

/// <summary>Result of mapping a route to the feature that owns it.</summary>
public sealed record RouteFeature(string? FeatureKey)
{
    public static readonly RouteFeature AlwaysOn = new((string?)null);

    public bool IsAlwaysOn => FeatureKey is null;
}

/// <summary>
/// Central, per-module map from API routes to feature flags, so every existing endpoint is
/// covered without editing each endpoint file. Endpoint metadata (<c>.RequireFeature</c> /
/// <c>.AlwaysOn</c>) wins over this map. An architecture test fails if any route is unmapped.
/// <para>
/// Rules match on path segments after <c>api/v1/</c>; <c>{}</c> matches any route parameter.
/// <b>Prefix</b> rules cover a sub-tree, <b>Exact</b> rules one template. The longest
/// matching rule wins; at equal length a method-specific rule beats an any-method rule.
/// </para>
/// </summary>
/// <summary>An HTTP method and route template (as written in <c>MapGet("...")</c>) to look up.</summary>
public sealed record RouteRequest(string Method, string Template);

public static class FeatureRouteMap
{
    private const string Any = "*";

    /// <summary>One map entry; a null <c>FeatureKey</c> means always on.</summary>
    private sealed record Rule(string? FeatureKey, string Path, string Method = Any, bool Exact = false);

    private sealed record CompiledRule(Rule Rule, string[] Segments, RouteFeature Feature);

    private sealed record RouteQuery(string Method, string[] Segments);

    private static readonly Rule[] Table =
    {
        // ---- Always-on core: auth, platform admin, notifications, flags, profiles, settings ----
        new(null, "identity"),
        new(null, "admin"),
        new(null, "observability"),
        new(null, "notifications"),
        new(null, "communication/notifications"),
        new(null, "communication/notification-preferences"),
        new(null, "feature-flags"),
        new(null, "billing/plan-features"),
        new(null, "billing/my-subscription"),
        new(null, "gyms", Exact: true),
        new(null, "gyms/pending", Exact: true),
        new(null, "gyms/{}", Exact: true),
        new(null, "gyms/{}/members"),
        new(null, "gyms/{}/staff"),
        new(null, "gyms/{}/trainers"),
        new(null, "gyms/{}/operating-hours"),
        new(null, "gyms/{}/seasonal-hours"),
        new(null, "gyms/{}/sections"),
        new(null, "gyms/{}/session-policy"),
        new(null, "gyms/{}/settings"),
        new(null, "owners/{}/gyms"),
        new(null, "gamification/trainers/{}/public-profile"),
        // Apps read branding to theme themselves; only editing it is a feature.
        new(null, "gyms/{}/branding", "GET", Exact: true),

        // ---- PT module ----
        new(K.PtPackages, "billing/package-templates"),
        new(K.PtPackages, "billing/packages"),
        new(K.PtPackages, "billing/me/packages"),
        new(K.PtTrialEndAlerts, "billing/trials"),
        new(K.PtDualSessionLogging, "scheduling/sessions", "GET", Exact: true),
        new(K.PtDualSessionLogging, "scheduling/sessions/upcoming"),
        new(K.PtDualSessionLogging, "scheduling/sessions/stats"),
        new(K.PtDualSessionLogging, "scheduling/sessions/{}", "GET", Exact: true),
        new(K.PtDualSessionLogging, "scheduling/sessions/{}/trainer-log"),
        new(K.PtDualSessionLogging, "scheduling/sessions/{}/member-log"),
        new(K.PtProgressMeasurements, "progress"),

        // ---- PT later ----
        new(K.PtCoachRatings, "gamification/trainer-ratings"),
        new(K.PtCoachRatings, "gamification/trainers/{}/ratings"),

        // ---- Everything else ----
        new(K.GymMemberships, "billing/plans"),
        new(K.GymMemberships, "billing/members/{}/balance"),
        new(K.GymMemberships, "gyms/{}/members/{}/freeze"),
        new(K.GymMemberships, "gyms/{}/members/{}/unfreeze"),
        new(K.GymMemberRecords, "gyms/{}/members/{}/notes"),
        new(K.GymMemberRecords, "gyms/{}/members/{}/onboarding"),
        new(K.GymAttendance, "gyms/{}/check-in"),
        new(K.GymAttendance, "gyms/{}/check-ins"),
        new(K.GymAttendance, "gyms/{}/kiosk-pin"),
        new(K.GymFinance, "billing/payments"),
        new(K.GymFinance, "billing/me/payments"),
        new(K.GymFinance, "billing/members/{}/payments"),
        new(K.GymFinance, "billing/revenue"),
        new(K.GymFinance, "billing/trainers"),
        new(K.GymFinance, "gyms/{}/trainers/{}/rate"),
        new(K.Classes, "gyms/{}/classes"),
        new(K.CommunicationAnnouncements, "gyms/{}/announcements"),
        new(K.CommunicationAnnouncements, "communication/broadcast"),
        new(K.CommunicationChat, "communication/conversations"),
        new(K.Analytics, "gyms/{}/analytics"),
        new(K.Analytics, "owners/{}/dashboard"),
        new(K.GymDataImport, "gyms/{}/imports"),
        new(K.GymDataImport, "gyms/imports"),
        new(K.GymBranding, "gyms/{}/branding"),
        new(K.SchedulingBooking, "scheduling/availability"),
        new(K.SchedulingBooking, "scheduling/available-slots"),
        new(K.SchedulingBooking, "scheduling/sessions", "POST", Exact: true),
        new(K.SchedulingBooking, "scheduling/sessions/recurring"),
        new(K.SchedulingBooking, "scheduling/sessions/{}/cancel"),
        new(K.SchedulingBooking, "scheduling/sessions/{}/no-show"),
        new(K.SchedulingBooking, "scheduling/sessions/{}/confirm"),
        new(K.SchedulingBooking, "scheduling/sessions/{}/start"),
        new(K.SchedulingBooking, "scheduling/sessions/{}/complete"),
        new(K.TrainingWorkouts, "scheduling/sessions/{}/exercise-logs"),
        new(K.TrainingWorkouts, "training"),
        new(K.TrainingPrograms, "training/programs"),
        new(K.TrainingPrograms, "training/assignments/program"),
        new(K.TrainingRemoteWorkout, "training/remote-workout"),
        new(K.Nutrition, "nutrition"),
        new(K.ProgressPhotos, "progress/photos"),
        new(K.ProgressInBody, "progress/body-metrics/inbody"),
        new(K.GamificationMemberLeaderboard, "gamification/leaderboards"),
        new(K.CoachLeaderboard, "gamification/trainers/leaderboard"),
        new(K.CoachLeaderboard, "gamification/trainers/coach-of-the-month"),
        new(K.CoachLeaderboard, "gamification/trainer-monthly-scores"),
        new(K.CoachTestimonials, "gamification/testimonials"),
        new(K.CoachTestimonials, "gamification/trainers/{}/testimonials"),
    };

    private static readonly CompiledRule[] Rules = Table
        .Select(r => new CompiledRule(
            r,
            r.Path.Split('/', StringSplitOptions.RemoveEmptyEntries),
            r.FeatureKey is null ? RouteFeature.AlwaysOn : new RouteFeature(r.FeatureKey)))
        .ToArray();

    static FeatureRouteMap()
    {
        var unknown = Table.FirstOrDefault(r => r.FeatureKey is not null && !FeatureRegistry.IsKnown(r.FeatureKey));
        if (unknown is not null)
            throw new InvalidOperationException($"FeatureRouteMap uses unknown flag '{unknown.FeatureKey}'.");
    }

    /// <summary>
    /// Feature owning the route, or null when no rule matches (an architecture test keeps that
    /// from happening). Routes outside <c>api/v1/</c> (health, hubs) are always on.
    /// </summary>
    public static RouteFeature? Resolve(RouteRequest request)
    {
        var segments = Normalize(request.Template);
        if (segments is null)
            return RouteFeature.AlwaysOn;

        var query = new RouteQuery(request.Method, segments);
        CompiledRule? best = null;
        foreach (var rule in Rules.Where(r => Matches(r, query)))
        {
            if (best is null || IsBetter(rule, best))
                best = rule;
        }

        return best?.Feature;
    }

    private static bool IsBetter(CompiledRule candidate, CompiledRule current)
    {
        if (candidate.Segments.Length != current.Segments.Length)
            return candidate.Segments.Length > current.Segments.Length;
        if (candidate.Rule.Exact != current.Rule.Exact)
            return candidate.Rule.Exact;
        return candidate.Rule.Method != Any && current.Rule.Method == Any;
    }

    private static bool Matches(CompiledRule rule, RouteQuery query)
    {
        if (rule.Rule.Method != Any && !string.Equals(rule.Rule.Method, query.Method, StringComparison.OrdinalIgnoreCase))
            return false;
        var lengthOk = rule.Rule.Exact
            ? query.Segments.Length == rule.Segments.Length
            : query.Segments.Length >= rule.Segments.Length;
        if (!lengthOk)
            return false;

        return rule.Segments
            .Zip(query.Segments)
            .All(pair => pair.First == "{}"
                ? pair.Second.StartsWith('{')
                : string.Equals(pair.First, pair.Second, StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>Segments after <c>api/v1/</c>, or null when the route is outside the versioned API.</summary>
    private static string[]? Normalize(string routeTemplate)
    {
        var trimmed = routeTemplate.Trim('/');
        const string prefix = "api/v1/";
        if (!trimmed.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
            return null;

        return trimmed[prefix.Length..].Split('/', StringSplitOptions.RemoveEmptyEntries);
    }
}
