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
public static class FeatureRouteMap
{
    private const string Any = "*";

    private sealed record Rule(string Method, string[] Segments, bool Exact, RouteFeature Feature);

    private static readonly List<Rule> Rules = new();

    static FeatureRouteMap()
    {
        // ---- Always-on core: auth, platform admin, notifications, flags, profiles, settings ----
        On("identity");
        On("admin");
        On("observability");
        On("notifications");
        On("communication/notifications");
        On("communication/notification-preferences");
        On("feature-flags");
        On("billing/plan-features");
        On("billing/my-subscription");
        OnExact(Any, "gyms");
        OnExact(Any, "gyms/pending");
        OnExact(Any, "gyms/{}");
        On("gyms/{}/members");
        On("gyms/{}/staff");
        On("gyms/{}/trainers");
        On("gyms/{}/operating-hours");
        On("gyms/{}/seasonal-hours");
        On("gyms/{}/sections");
        On("gyms/{}/session-policy");
        On("gyms/{}/settings");
        On("owners/{}/gyms");
        On("gamification/trainers/{}/public-profile");
        // Apps read branding to theme themselves; only editing it is a feature.
        OnExact("GET", "gyms/{}/branding");

        // ---- PT module ----
        Flag(K.PtPackages, "billing/package-templates");
        Flag(K.PtPackages, "billing/packages");
        Flag(K.PtPackages, "billing/me/packages");
        Flag(K.PtTrialEndAlerts, "billing/trials");
        Flag(K.PtDualSessionLogging, "scheduling/sessions", exact: true, method: "GET");
        Flag(K.PtDualSessionLogging, "scheduling/sessions/upcoming");
        Flag(K.PtDualSessionLogging, "scheduling/sessions/stats");
        Flag(K.PtDualSessionLogging, "scheduling/sessions/{}", exact: true, method: "GET");
        Flag(K.PtDualSessionLogging, "scheduling/sessions/{}/trainer-log");
        Flag(K.PtDualSessionLogging, "scheduling/sessions/{}/member-log");
        Flag(K.PtProgressMeasurements, "progress");

        // ---- PT later ----
        Flag(K.PtCoachRatings, "gamification/trainer-ratings");
        Flag(K.PtCoachRatings, "gamification/trainers/{}/ratings");

        // ---- Everything else ----
        Flag(K.GymMemberships, "billing/plans");
        Flag(K.GymMemberships, "billing/members/{}/balance");
        Flag(K.GymMemberships, "gyms/{}/members/{}/freeze");
        Flag(K.GymMemberships, "gyms/{}/members/{}/unfreeze");
        Flag(K.GymMemberRecords, "gyms/{}/members/{}/notes");
        Flag(K.GymMemberRecords, "gyms/{}/members/{}/onboarding");
        Flag(K.GymAttendance, "gyms/{}/check-in");
        Flag(K.GymAttendance, "gyms/{}/check-ins");
        Flag(K.GymAttendance, "gyms/{}/kiosk-pin");
        Flag(K.GymFinance, "billing/payments");
        Flag(K.GymFinance, "billing/me/payments");
        Flag(K.GymFinance, "billing/members/{}/payments");
        Flag(K.GymFinance, "billing/revenue");
        Flag(K.GymFinance, "billing/trainers");
        Flag(K.GymFinance, "gyms/{}/trainers/{}/rate");
        Flag(K.Classes, "gyms/{}/classes");
        Flag(K.CommunicationAnnouncements, "gyms/{}/announcements");
        Flag(K.CommunicationAnnouncements, "communication/broadcast");
        Flag(K.CommunicationChat, "communication/conversations");
        Flag(K.Analytics, "gyms/{}/analytics");
        Flag(K.Analytics, "owners/{}/dashboard");
        Flag(K.GymDataImport, "gyms/{}/imports");
        Flag(K.GymDataImport, "gyms/imports");
        Flag(K.GymBranding, "gyms/{}/branding");
        Flag(K.SchedulingBooking, "scheduling/availability");
        Flag(K.SchedulingBooking, "scheduling/available-slots");
        Flag(K.SchedulingBooking, "scheduling/sessions", exact: true, method: "POST");
        Flag(K.SchedulingBooking, "scheduling/sessions/recurring");
        Flag(K.SchedulingBooking, "scheduling/sessions/{}/cancel");
        Flag(K.SchedulingBooking, "scheduling/sessions/{}/no-show");
        Flag(K.SchedulingBooking, "scheduling/sessions/{}/confirm");
        Flag(K.SchedulingBooking, "scheduling/sessions/{}/start");
        Flag(K.SchedulingBooking, "scheduling/sessions/{}/complete");
        Flag(K.TrainingWorkouts, "scheduling/sessions/{}/exercise-logs");
        Flag(K.TrainingWorkouts, "training");
        Flag(K.TrainingPrograms, "training/programs");
        Flag(K.TrainingPrograms, "training/assignments/program");
        Flag(K.TrainingRemoteWorkout, "training/remote-workout");
        Flag(K.Nutrition, "nutrition");
        Flag(K.ProgressPhotos, "progress/photos");
        Flag(K.GamificationMemberLeaderboard, "gamification/leaderboards");
        Flag(K.CoachLeaderboard, "gamification/trainers/leaderboard");
        Flag(K.CoachLeaderboard, "gamification/trainers/coach-of-the-month");
        Flag(K.CoachLeaderboard, "gamification/trainer-monthly-scores");
        Flag(K.CoachTestimonials, "gamification/testimonials");
        Flag(K.CoachTestimonials, "gamification/trainers/{}/testimonials");

        foreach (var rule in Rules.Where(r => r.Feature.FeatureKey is not null))
        {
            if (!FeatureRegistry.IsKnown(rule.Feature.FeatureKey!))
                throw new InvalidOperationException($"FeatureRouteMap uses unknown flag '{rule.Feature.FeatureKey}'.");
        }
    }

    /// <summary>
    /// Feature owning <paramref name="routeTemplate"/> (as written in <c>MapGet("...")</c>), or
    /// null when no rule matches (an architecture test keeps that from happening). Routes outside
    /// <c>api/v1/</c> (health, hubs) are always on.
    /// </summary>
    public static RouteFeature? Resolve(string httpMethod, string routeTemplate)
    {
        var segments = Normalize(routeTemplate);
        if (segments is null)
            return RouteFeature.AlwaysOn;

        Rule? best = null;
        foreach (var rule in Rules.Where(r => Matches(r, httpMethod, segments)))
        {
            if (best is null || IsBetter(rule, best))
                best = rule;
        }

        return best?.Feature;
    }

    private static bool IsBetter(Rule candidate, Rule current)
    {
        if (candidate.Segments.Length != current.Segments.Length)
            return candidate.Segments.Length > current.Segments.Length;
        if (candidate.Exact != current.Exact)
            return candidate.Exact;
        return candidate.Method != Any && current.Method == Any;
    }

    private static bool Matches(Rule rule, string method, string[] segments)
    {
        if (rule.Method != Any && !string.Equals(rule.Method, method, StringComparison.OrdinalIgnoreCase))
            return false;
        if (rule.Exact ? segments.Length != rule.Segments.Length : segments.Length < rule.Segments.Length)
            return false;

        for (var i = 0; i < rule.Segments.Length; i++)
        {
            var expected = rule.Segments[i];
            var actual = segments[i];
            var matches = expected == "{}"
                ? actual.StartsWith('{')
                : string.Equals(expected, actual, StringComparison.OrdinalIgnoreCase);
            if (!matches)
                return false;
        }

        return true;
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

    private static string[] Split(string path) => path.Split('/', StringSplitOptions.RemoveEmptyEntries);

    private static void On(string prefix) =>
        Rules.Add(new Rule(Any, Split(prefix), Exact: false, RouteFeature.AlwaysOn));

    private static void OnExact(string method, string path) =>
        Rules.Add(new Rule(method, Split(path), Exact: true, RouteFeature.AlwaysOn));

    private static void Flag(string key, string path, bool exact = false, string method = Any) =>
        Rules.Add(new Rule(method, Split(path), exact, new RouteFeature(key)));
}
