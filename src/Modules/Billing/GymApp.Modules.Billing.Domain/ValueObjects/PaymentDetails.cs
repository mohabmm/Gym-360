using GymApp.Modules.Billing.Domain.Enums;

namespace GymApp.Modules.Billing.Domain.ValueObjects;

/// <summary>A recorded payment: who paid, how much, how, for what, and who recorded it.</summary>
public sealed record PaymentDetails
{
    public required Guid GymId { get; init; }
    public required Guid MemberId { get; init; }
    public required Money Amount { get; init; }
    public required PaymentMethod PaymentMethod { get; init; }
    public required PaymentPurpose Purpose { get; init; }
    public required string ReceiptNumber { get; init; }
    public required Guid RecordedBy { get; init; }
    public Guid? ReferenceId { get; init; }
}
