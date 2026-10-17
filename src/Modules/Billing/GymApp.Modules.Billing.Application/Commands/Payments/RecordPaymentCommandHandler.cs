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
        var referenceId = await ResolveReferenceIdAsync(request, purpose, amount, cancellationToken);

        var payment = Payment.Create(new PaymentDetails
        {
            GymId = request.GymId,
            MemberId = request.MemberId,
            Amount = amount,
            PaymentMethod = paymentMethod,
            Purpose = purpose,
            ReceiptNumber = receiptNumber,
            RecordedBy = request.RecordedBy,
            ReferenceId = referenceId
        });

        await _paymentRepository.AddAsync(payment, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success(payment.Id);
    }

    // Bidirectional assign-trainer flow: caller records a SessionPackage
    // payment without a pre-existing package — create the package
    // atomically, link payment.ReferenceId to it, derive PricePerSession
    // from Amount/SessionCount so trainer earnings + remaining sessions
    // are correct downstream.
    private async Task<Guid?> ResolveReferenceIdAsync(
        RecordPaymentCommand request,
        PaymentPurpose purpose,
        Money amount,
        CancellationToken cancellationToken)
    {
        if (!IsCustomSessionPackagePayment(request, purpose))
            return request.ReferenceId;

        var sessionCount = request.SessionCount!.Value;
        var package = SessionPackage.Create(new SessionPackageDetails
        {
            GymId = request.GymId,
            TrainerId = request.TrainerId!.Value,
            MemberId = request.MemberId,
            TemplateId = Guid.Empty,
            Name = $"Custom: {sessionCount} sessions",
            TotalSessions = sessionCount,
            PricePerSession = Money.Create(request.Amount / sessionCount),
            TotalPrice = amount,
            AssignedBy = request.RecordedBy,
            ExpiresAt = null
        });

        await _sessionPackageRepository.AddAsync(package, cancellationToken);
        return package.Id;
    }

    private static bool IsCustomSessionPackagePayment(RecordPaymentCommand request, PaymentPurpose purpose) =>
        purpose == PaymentPurpose.SessionPackage
        && !request.ReferenceId.HasValue
        && HasCustomPackageTerms(request);

    private static bool HasCustomPackageTerms(RecordPaymentCommand request) =>
        request.TrainerId.HasValue && request.SessionCount is > 0;
}
