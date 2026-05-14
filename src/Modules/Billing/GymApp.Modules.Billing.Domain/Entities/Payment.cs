using GymApp.Common.Domain;
using GymApp.Modules.Billing.Domain.Enums;
using GymApp.Modules.Billing.Domain.Events;
using GymApp.Modules.Billing.Domain.ValueObjects;

namespace GymApp.Modules.Billing.Domain.Entities;

public sealed class Payment : Entity
{
    private Payment() { }

    public Guid GymId { get; private set; }
    public Guid MemberId { get; private set; }
    public Money Amount { get; private set; } = default!;
    public PaymentMethod PaymentMethod { get; private set; }
    public PaymentPurpose Purpose { get; private set; }
    public Guid? ReferenceId { get; private set; }
    public PaymentStatus Status { get; private set; }
    public string ReceiptNumber { get; private set; } = default!;
    public Guid RecordedBy { get; private set; }
    public string? VoidReason { get; private set; }
    public DateTime? PaidAt { get; private set; }
    public DateTime CreatedAt { get; private set; }

    public static Payment Create(
        Guid gymId,
        Guid memberId,
        Money amount,
        PaymentMethod paymentMethod,
        PaymentPurpose purpose,
        string receiptNumber,
        Guid recordedBy,
        Guid? referenceId = null)
    {
        var payment = new Payment
        {
            Id = Guid.NewGuid(),
            GymId = gymId,
            MemberId = memberId,
            Amount = amount,
            PaymentMethod = paymentMethod,
            Purpose = purpose,
            ReferenceId = referenceId,
            Status = PaymentStatus.Completed,
            ReceiptNumber = receiptNumber,
            RecordedBy = recordedBy,
            PaidAt = DateTime.UtcNow,
            CreatedAt = DateTime.UtcNow
        };

        payment.RaiseDomainEvent(new PaymentReceivedDomainEvent(
            payment.Id,
            payment.GymId,
            payment.Amount.Amount));

        return payment;
    }

    public Result Void(string reason)
    {
        if (Status == PaymentStatus.Voided)
            return Result.Failure(Errors.BillingErrors.PaymentAlreadyVoided);

        Status = PaymentStatus.Voided;
        VoidReason = reason;

        RaiseDomainEvent(new PaymentVoidedDomainEvent(Id, GymId, reason));

        return Result.Success();
    }
}
