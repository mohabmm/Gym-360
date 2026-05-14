using GymApp.Common.Application.Abstractions;
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
    private readonly IGymPlanService _gymPlanService;
    private readonly IBillingUnitOfWork _unitOfWork;

    public QuickAssignSessionPackageCommandHandler(
        ISessionPackageRepository packageRepository,
        IPaymentRepository paymentRepository,
        IGymPlanService gymPlanService,
        IBillingUnitOfWork unitOfWork)
    {
        _packageRepository = packageRepository;
        _paymentRepository = paymentRepository;
        _gymPlanService = gymPlanService;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<Guid>> Handle(QuickAssignSessionPackageCommand request, CancellationToken cancellationToken)
    {
        var enabled = await _gymPlanService.IsFeatureEnabledAsync(
            request.GymId, "session_packages", cancellationToken);
        if (!enabled)
        {
            var plan = await _gymPlanService.GetPlanCodeAsync(request.GymId, cancellationToken);
            return Result.Failure<Guid>(PlanErrors.FeatureNotAvailable("session_packages", plan, "Pro"));
        }

        var totalAmount = request.TotalPrice ?? 0m;
        // Caller supplies the lump-sum package price; per-session is derived
        // so trainer-earnings views that read pricePerSession still work.
        var pricePerAmount = request.SessionCount > 0
            ? totalAmount / request.SessionCount
            : 0m;
        var pricePer = Money.Create(pricePerAmount);
        var total = Money.Create(totalAmount);

        // No FK between session_packages.TemplateId and session_package_templates,
        // so quick-assign passes Guid.Empty to mean "ad-hoc, no template".
        var package = SessionPackage.Create(
            request.GymId,
            request.TrainerId,
            request.MemberId,
            templateId: Guid.Empty,
            name: $"Custom: {request.SessionCount} sessions",
            totalSessions: request.SessionCount,
            pricePerSession: pricePer,
            totalPrice: total,
            assignedBy: request.AssignedBy,
            expiresAt: request.ExpiresAt);

        await _packageRepository.AddAsync(package, cancellationToken);

        // Mirror the assignment in the payments table so revenue dashboards,
        // member payment history, and trainer earnings reflect the sale in
        // the same transaction. Skips the payment row when price is zero
        // (free assignment) or when caller didn't supply a payment method.
        if (totalAmount > 0m && !string.IsNullOrWhiteSpace(request.PaymentMethod))
        {
            if (!Enum.TryParse<PaymentMethod>(request.PaymentMethod, true, out var method))
                return Result.Failure<Guid>(new Error("Billing.InvalidPaymentMethod", "Invalid payment method."));

            var receiptNumber = $"RCP-{DateTime.UtcNow:yyyyMMdd}-{Guid.NewGuid().ToString()[..8].ToUpper()}";
            var payment = Payment.Create(
                request.GymId,
                request.MemberId,
                total,
                method,
                PaymentPurpose.SessionPackage,
                receiptNumber,
                request.AssignedBy,
                package.Id);
            await _paymentRepository.AddAsync(payment, cancellationToken);
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success(package.Id);
    }
}
