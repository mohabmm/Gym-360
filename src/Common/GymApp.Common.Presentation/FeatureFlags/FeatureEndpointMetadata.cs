using Microsoft.AspNetCore.Builder;

namespace GymApp.Common.Presentation.FeatureFlags;

/// <summary>Endpoint belongs to a flagged feature; calls for a gym with it OFF get 403 FEATURE_DISABLED:&lt;key&gt;.</summary>
public sealed record RequireFeatureMetadata(string FeatureKey);

/// <summary>Endpoint is part of the always-on core and is never feature-gated.</summary>
public sealed record AlwaysOnFeatureMetadata;

public static class FeatureEndpointExtensions
{
    /// <summary>
    /// Gates the endpoint behind a feature flag. Takes precedence over the central route map,
    /// so new endpoints should declare their flag here.
    /// </summary>
    public static TBuilder RequireFeature<TBuilder>(this TBuilder builder, string featureKey)
        where TBuilder : IEndpointConventionBuilder =>
        builder.WithMetadata(new RequireFeatureMetadata(featureKey));

    /// <summary>Marks the endpoint as always on (core: auth, profiles, settings, notifications).</summary>
    public static TBuilder AlwaysOn<TBuilder>(this TBuilder builder)
        where TBuilder : IEndpointConventionBuilder =>
        builder.WithMetadata(new AlwaysOnFeatureMetadata());
}
