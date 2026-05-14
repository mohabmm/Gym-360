using GymApp.Common.Domain;

namespace GymApp.Modules.Billing.Domain.Errors;

public static class BillingErrors
{
    public static readonly Error PaymentNotFound = new(
        "Billing.PaymentNotFound", "The specified payment was not found.");

    public static readonly Error PlanNotFound = new(
        "Billing.PlanNotFound", "The specified membership plan was not found.");

    public static readonly Error PackageNotFound = new(
        "Billing.PackageNotFound", "The specified session package was not found.");

    public static readonly Error TemplateNotFound = new(
        "Billing.TemplateNotFound", "The specified session package template was not found.");

    public static readonly Error FeatureNotFound = new(
        "Billing.FeatureNotFound", "The specified feature was not found.");

    public static readonly Error PaymentAlreadyVoided = new(
        "Billing.PaymentAlreadyVoided", "The payment has already been voided.");

    public static readonly Error PackageExhausted = new(
        "Billing.PackageExhausted", "The session package has no remaining sessions.");

    public static readonly Error PackageExpired = new(
        "Billing.PackageExpired", "The session package has expired.");

    public static readonly Error SubscriptionNotFound = new(
        "Billing.SubscriptionNotFound", "The specified gym subscription was not found.");

    public static readonly Error GymAlreadySubscribed = new(
        "Billing.GymAlreadySubscribed", "The gym already has an active subscription.");

    public static readonly Error PlanNotActive = new(
        "Billing.PlanNotActive", "The specified plan is not active.");

    public static readonly Error PlanNotPublic = new(
        "Billing.PlanNotPublic", "The specified plan is not publicly available.");

    public static readonly Error FeatureCodeAlreadyExists = new(
        "Billing.FeatureCodeAlreadyExists", "A feature with this code already exists.");

    public static readonly Error PlanCodeAlreadyExists = new(
        "Billing.PlanCodeAlreadyExists", "A subscription plan with this code already exists.");

    public static readonly Error OverrideAlreadyExists = new(
        "Billing.OverrideAlreadyExists", "A feature override already exists for this gym and feature.");

    public static readonly Error CannotDeletePlanWithActiveSubscriptions = new(
        "Billing.CannotDeletePlanWithActiveSubscriptions",
        "Cannot delete a plan that has active subscriptions.");
}
