using GymApp.Modules.Billing.Shared;
using GymApp.Common.Application.Errors;
using GymApp.Common.Application.Messaging;
using GymApp.Common.Domain;
using GymApp.Modules.Billing.Domain.Entities;
using GymApp.Modules.Billing.Domain.Enums;
using GymApp.Modules.Billing.Domain.Repositories;
using GymApp.Modules.Billing.Domain.ValueObjects;
using GymApp.Modules.Billing.Application.Abstractions;

namespace GymApp.Modules.Billing.Application.Commands.Payments;

internal sealed class RecordPaymentCommandHandler : ICommandHandler<RecordPaymentCommand, Guid>
{
    private readonly IPaymentRepository _paymentRepository;
    private readonly ISessionPackageRepository _sessionPackageRepository;
    private readonly IGymPlanService _gymPlanService;
    private readonly IBillingUnitOfWork _unitOfWork;

    public RecordPaymentCommandHandler(
        IPaymentRepository paymentRepository,
        ISessionPackageRepository sessionPackageRepository,
        IGymPlanService gymPlanService,
        IBillingUnitOfWork unitOfWork)
    {
        _paymentRepository = paymentRepository;
        _sessionPackageRepository = sessionPackageRepository;
        _gymPlanService = gymPlanService;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<Guid>> Handle(RecordPaymentCommand request, CancellationToken cancellationToken)
    {
        bool isEnabled = await _gymPlanService.IsFeatureEnabledAsync(
            request.GymId, "payment_recording", cancellationToken);

        if (!isEnabled)
        {
            string currentPlan = await _gymPlanService.GetPlanCodeAsync(request.GymId, cancellationToken);
            return Result.Failure<Guid>(PlanErrors.FeatureNotAvailable("payment_recording", currentPlan, "Starter"));
        }

        if (!Enum.TryParse<PaymentMethod>(request.PaymentMethod, true, out var paymentMethod))
        {
            return Result.Failure<Guid>(new Error("Billing.InvalidPaymentMethod", "Invalid payment method."));
        }

        if (!Enum.TryParse<PaymentPurpose>(request.Purpose, true, out var purpose))
        {
            return Result.Failure<Guid>(new Error("Billing.InvalidPurpose", "Invalid payment purpose."));
        }

        var amount = Money.Create(request.Amount);
        var receiptNumber = $"RCP-{DateTime.UtcNow:yyyyMMdd}-{Guid.NewGuid().ToString()[..8].ToUpper()}";

        // Bidirectional assign-trainer flow: caller records a SessionPackage
        // payment without a pre-existing package — create the package
        // atomically, link payment.ReferenceId to it, derive PricePerSession
        // from Amount/SessionCount so trainer earnings + remaining sessions
        // are correct downstream.
        var resolvedReferenceId = request.ReferenceId;
        if (purpose == PaymentPurpose.SessionPackage
            && !resolvedReferenceId.HasValue
            && request.TrainerId.HasValue
            && request.SessionCount.HasValue
            && request.SessionCount.Value > 0)
        {
            var sessionCount = request.SessionCount.Value;
            var pricePer = Money.Create(request.Amount / sessionCount);
            var package = SessionPackage.Create(
                request.GymId,
                request.TrainerId.Value,
                request.MemberId,
                templateId: Guid.Empty,
                name: $"Custom: {sessionCount} sessions",
                totalSessions: sessionCount,
                pricePerSession: pricePer,
                totalPrice: amount,
                assignedBy: request.RecordedBy,
                expiresAt: null);

            await _sessionPackageRepository.AddAsync(package, cancellationToken);
            resolvedReferenceId = package.Id;
        }

        var payment = Payment.Create(
            request.GymId,
            request.MemberId,
            amount,
            paymentMethod,
            purpose,
            receiptNumber,
            request.RecordedBy,
            resolvedReferenceId);

        await _paymentRepository.AddAsync(payment, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success(payment.Id);
    }
}
