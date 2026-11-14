using GymApp.Modules.Billing.Domain.Errors;
using GymApp.Common.Application.FeatureFlags;
using GymApp.Modules.Billing.Shared;
using GymApp.Common.Application.Errors;
using GymApp.Common.Application.Messaging;
using GymApp.Common.Domain;
using GymApp.Modules.Billing.Application.Abstractions;
using GymApp.Modules.Billing.Domain.Entities;
using GymApp.Modules.Billing.Domain.Enums;
using GymApp.Modules.Billing.Domain.Repositories;
using GymApp.Modules.Billing.Domain.ValueObjects;

namespace GymApp.Modules.Billing.Application.Commands.SessionPackages;

internal sealed class QuickAssignSessionPackageCommandHandler
    : ICommandHandler<QuickAssignSessionPackageCommand, Guid>
{
    private readonly ISessionPackageRepository _packageRepository;
    private readonly IPaymentRepository _paymentRepository;
    private readonly IFeatureFlagService _featureFlags;
    private readonly IBillingUnitOfWork _unitOfWork;

    public QuickAssignSessionPackageCommandHandler(
        ISessionPackageRepository packageRepository,
        IPaymentRepository paymentRepository,
        IFeatureFlagService featureFlags,
        IBillingUnitOfWork unitOfWork)
    {
        _packageRepository = packageRepository;
        _paymentRepository = paymentRepository;
        _featureFlags = featureFlags;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<Guid>> Handle(QuickAssignSessionPackageCommand request, CancellationToken cancellationToken)
    {
        var gate = await CheckFeaturesAsync(request, cancellationToken);
        if (gate.IsFailure)
            return Result.Failure<Guid>(gate.Error);

        // One coach at a time: a member whose current package still has sessions left
        // cannot be given another coach until those sessions are used (or expire).
        if (await _packageRepository.HasActivePackageAsync(
                new PackageOwner(request.GymId, request.MemberId), DateTime.UtcNow, cancellationToken))
            return Result.Failure<Guid>(BillingErrors.CoachAlreadyAssigned);

        // While pt.auto_expiry is off (default) packages never expire, so none gets an expiry date.
        var expiresAt = await _featureFlags.IsEnabledAsync(request.GymId, FeatureKeys.PtAutoExpiry, cancellationToken)
            ? request.ExpiresAt
            : null;

        var totalAmount = request.TotalPrice ?? 0m;
        var total = Money.Create(totalAmount);

        // No FK between session_packages.TemplateId and session_package_templates,
        // so quick-assign passes Guid.Empty to mean "ad-hoc, no template".
        var package = SessionPackage.Create(new SessionPackageDetails
        {
            GymId = request.GymId,
            TrainerId = request.TrainerId,
            MemberId = request.MemberId,
            TemplateId = Guid.Empty,
            Name = PackageName(request, totalAmount),
            TotalSessions = request.SessionCount,
            PricePerSession = PricePerSession(totalAmount, request.SessionCount),
            TotalPrice = total,
            AssignedBy = request.AssignedBy,
            ExpiresAt = expiresAt,
            IsTrial = request.IsTrial
        });

        await _packageRepository.AddAsync(package, cancellationToken);

        var paymentResult = await RecordPaymentAsync(request, package, cancellationToken);
        if (paymentResult.IsFailure)
            return Result.Failure<Guid>(paymentResult.Error);

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success(package.Id);
    }

    // Free desk-assigned sessions are labelled as such so staff and the coach can tell them apart.
    private static string PackageName(QuickAssignSessionPackageCommand request, decimal totalAmount) =>
        (request.IsTrial, totalAmount > 0m) switch
        {
            (true, _) => $"Trial: {request.SessionCount} free sessions",
            (false, false) => $"Free: {request.SessionCount} sessions",
            _ => $"Custom: {request.SessionCount} sessions"
        };

    /// <summary>
    /// Caller supplies the lump-sum package price; per-session is derived so trainer-earnings
    /// views that read pricePerSession still work.
    /// </summary>
    private static Money PricePerSession(decimal totalAmount, int sessionCount) =>
        Money.Create(sessionCount > 0 ? totalAmount / sessionCount : 0m);

    /// <summary>
    /// Mirrors the assignment in the payments table so revenue dashboards, member payment
    /// history, and trainer earnings reflect the sale in the same transaction. Skips the payment
    /// row when price is zero (free assignment) or when the caller didn't supply a payment method.
    /// </summary>
    private async Task<Result> RecordPaymentAsync(
        QuickAssignSessionPackageCommand request, SessionPackage package, CancellationToken cancellationToken)
    {
        var totalAmount = request.TotalPrice ?? 0m;
        if (totalAmount <= 0m || string.IsNullOrWhiteSpace(request.PaymentMethod))
            return Result.Success();

        if (!Enum.TryParse<PaymentMethod>(request.PaymentMethod, true, out var method))
            return Result.Failure(new Error("Billing.InvalidPaymentMethod", "Invalid payment method."));

        var receiptNumber = $"RCP-{DateTime.UtcNow:yyyyMMdd}-{Guid.NewGuid().ToString()[..8].ToUpper()}";
        var payment = Payment.Create(new PaymentDetails
        {
            GymId = request.GymId,
            MemberId = request.MemberId,
            Amount = Money.Create(totalAmount),
            PaymentMethod = method,
            Purpose = PaymentPurpose.SessionPackage,
            ReceiptNumber = receiptNumber,
            RecordedBy = request.AssignedBy,
            ReferenceId = package.Id
        });
        await _paymentRepository.AddAsync(payment, cancellationToken);
        return Result.Success();
    }

    private async Task<Result> CheckFeaturesAsync(QuickAssignSessionPackageCommand request, CancellationToken cancellationToken)
    {
        if (!await _featureFlags.IsEnabledAsync(request.GymId, FeatureKeys.PtPackages, cancellationToken))
            return Result.Failure(FeatureFlagErrors.FeatureDisabled(FeatureKeys.PtPackages));

        if (!request.IsTrial)
            return Result.Success();

        if (!await _featureFlags.IsEnabledAsync(request.GymId, FeatureKeys.PtTrialSessions, cancellationToken))
            return Result.Failure(FeatureFlagErrors.FeatureDisabled(FeatureKeys.PtTrialSessions));

        return await _packageRepository.HasTrialAsync(new PackageOwner(request.GymId, request.MemberId), cancellationToken)
            ? Result.Failure(BillingErrors.TrialAlreadyUsed)
            : Result.Success();
    }
}
