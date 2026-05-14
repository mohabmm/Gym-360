using GymApp.Common.Domain;

namespace GymApp.Common.Application.Errors;

/// <summary>
/// Plan / feature-gating error codes per docs/01_GYMAPP_BACKEND_COMPLETE.md §"Error Response for Gated Features".
/// Clients (mobile + web) branch on <c>errors[0].code</c> to show upgrade prompts; the meta payload
/// carries the feature code and plan identifiers so the UI can render the correct CTA without an
/// extra round-trip.
/// </summary>
public static class PlanErrors
{
    public static Error FeatureNotAvailable(string featureCode, string currentPlan, string requiredPlan) =>
        new("PLAN.FEATURE_NOT_AVAILABLE",
            $"The '{featureCode}' feature requires the {requiredPlan} plan or higher. Current plan: {currentPlan}.",
            new
            {
                feature = featureCode,
                currentPlan,
                requiredPlan,
                upgradeUrl = "/api/v1/billing/upgrade",
            });

    public static Error LimitExceeded(string resource, int currentUsage, int limit) =>
        new("PLAN.LIMIT_EXCEEDED",
            $"The {resource} limit has been reached. Current: {currentUsage}, Limit: {limit}.",
            new
            {
                resource,
                currentUsage,
                limit,
            });

    public static Error StorageLimitReached(string resource, long limitValue) =>
        new("PLAN.STORAGE_LIMIT_REACHED",
            $"Storage limit reached for {resource}. Limit: {limitValue}. Upgrade your plan for more capacity.",
            new
            {
                resource,
                limit = limitValue,
            });
}
