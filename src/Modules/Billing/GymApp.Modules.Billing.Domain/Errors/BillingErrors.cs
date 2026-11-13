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

    public static readonly Error TrialAlreadyUsed = new(
        "Billing.Trial.AlreadyExists", "This trainee already had a free trial.");

    public static readonly Error TrialRecapNotATrial = new(
        "Billing.TrialRecap.NotATrial", "A recap can only be sent for a free trial package.");

    public static readonly Error TrialRecapForbidden = new(
        "Billing.TrialRecap.Forbidden", "Only the trial's coach can send its recap.");

    public static readonly Error TrialRecapInvalidNote = new(
        "Billing.TrialRecap.InvalidNote", "The recap note is required and must be at most 500 characters.");

    public static readonly Error TrialRecapInvalidSessions = new(
        "Billing.TrialRecap.InvalidSessions", "Recommended sessions must be a multiple of 4 between 4 and 40.");

    public static readonly Error TrialRecapNotFound = new(
        "Billing.TrialRecapNotFound", "No trial recap has been sent yet.");

    public static readonly Error PackageInterestNotFound = new(
        "Billing.PackageInterestNotFound", "The specified package interest was not found.");

    public static readonly Error PackageInterestNotOpen = new(
        "Billing.PackageInterest.NotOpen", "This lead is already closed.");

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
