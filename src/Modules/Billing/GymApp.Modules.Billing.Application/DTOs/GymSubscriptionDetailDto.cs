namespace GymApp.Modules.Billing.Application.DTOs;

public sealed record GymSubscriptionDetailDto(
    Guid Id,
    Guid GymId,
    Guid PlanId,
    string PlanCode,
    string PlanName,
    DateTime StartedAt,
    DateTime? ExpiresAt,
    bool IsActive,
    bool IsTrial,
    DateTime? TrialExpiresAt,
    string PaymentStatus,
    DateTime? LastPaymentAt,
    DateTime? NextPaymentDue,
    DateTime? OverdueSince,
    DateTime? GracePeriodEndsAt);
