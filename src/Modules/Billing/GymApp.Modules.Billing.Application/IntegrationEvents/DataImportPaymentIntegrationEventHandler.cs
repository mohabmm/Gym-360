using GymApp.Modules.Identity.Shared;
using GymApp.Modules.Billing.Application.Abstractions;
using GymApp.Modules.Billing.Domain.Entities;
using GymApp.Modules.Billing.Domain.Enums;
using GymApp.Modules.Billing.Domain.Repositories;
using GymApp.Modules.Billing.Domain.ValueObjects;
using GymApp.Modules.Gyms.IntegrationEvents;
using MediatR;
using Microsoft.Extensions.Logging;

namespace GymApp.Modules.Billing.Application.IntegrationEvents;

internal sealed class DataImportPaymentIntegrationEventHandler : INotificationHandler<DataImportPaymentIntegrationEvent>
{
    private readonly IPaymentRepository _paymentRepository;
    private readonly IGymMemberLookup _memberLookup;
    private readonly IBillingUnitOfWork _unitOfWork;
    private readonly ILogger<DataImportPaymentIntegrationEventHandler> _logger;

    public DataImportPaymentIntegrationEventHandler(
        IPaymentRepository paymentRepository,
        IGymMemberLookup memberLookup,
        IBillingUnitOfWork unitOfWork,
        ILogger<DataImportPaymentIntegrationEventHandler> logger)
    {
        _paymentRepository = paymentRepository;
        _memberLookup = memberLookup;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task Handle(DataImportPaymentIntegrationEvent n, CancellationToken ct)
    {
        var memberId = await _memberLookup.GetUserIdByMemberNumberAsync(n.GymId, n.MemberNumber, ct);
        if (memberId is null)
        {
            _logger.LogWarning(
                "DataImport job {JobId} row {Row}: member '{MemberNumber}' not found in gym {GymId} — payment skipped",
                n.ImportJobId, n.RowNumber, n.MemberNumber, n.GymId);
            return;
        }

        if (!Enum.TryParse<PaymentMethod>(n.PaymentMethod, ignoreCase: true, out var method))
            method = PaymentMethod.Cash;

        if (!Enum.TryParse<PaymentPurpose>(n.Purpose, ignoreCase: true, out var purpose))
            purpose = PaymentPurpose.Membership;

        var amount = Money.Create(n.Amount, n.Currency);

        var payment = Payment.Create(
            n.GymId,
            memberId.Value,
            amount,
            method,
            purpose,
            n.ReceiptNumber,
            recordedBy: memberId.Value);

        await _paymentRepository.AddAsync(payment, ct);
        await _unitOfWork.SaveChangesAsync(ct);
    }
}
