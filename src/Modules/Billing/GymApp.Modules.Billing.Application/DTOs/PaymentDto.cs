namespace GymApp.Modules.Billing.Application.DTOs;

public sealed record PaymentDto(
    Guid Id,
    Guid GymId,
    Guid MemberId,
    decimal Amount,
    string Currency,
    string PaymentMethod,
    string Purpose,
    Guid? ReferenceId,
    string Status,
    string ReceiptNumber,
    Guid RecordedBy,
    string? VoidReason,
    DateTime? PaidAt,
    DateTime CreatedAt,
    string? MemberName = null);
