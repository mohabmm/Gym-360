namespace GymApp.Modules.Billing.Application.DTOs;

/// <summary>
/// Trainee-facing projection of a Payment as defined by the
/// `/api/v1/billing/me/payments` PRD. Includes Voided payments (Q9): the
/// amount is always positive and carries VoidReason + VoidedAt when Voided.
/// </summary>
public sealed record MyPaymentDto(
    Guid Id,
    string ReceiptNumber,
    MoneyDto Amount,
    string PaymentMethod,
    string Purpose,
    Guid? ReferenceId,
    string? ReferenceLabel,
    string Status,
    DateTime? PaidAt,
    string? RecordedByStaffName,
    string? VoidReason,
    DateTime? VoidedAt);
