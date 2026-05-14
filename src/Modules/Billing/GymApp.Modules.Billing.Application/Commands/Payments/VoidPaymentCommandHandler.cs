using GymApp.Common.Application.Messaging;
using GymApp.Common.Domain;
using GymApp.Modules.Billing.Domain.Errors;
using GymApp.Modules.Billing.Domain.Repositories;
using GymApp.Modules.Billing.Application.Abstractions;

namespace GymApp.Modules.Billing.Application.Commands.Payments;

internal sealed class VoidPaymentCommandHandler : ICommandHandler<VoidPaymentCommand>
{
    private readonly IPaymentRepository _paymentRepository;
    private readonly IBillingUnitOfWork _unitOfWork;

    public VoidPaymentCommandHandler(
        IPaymentRepository paymentRepository,
        IBillingUnitOfWork unitOfWork)
    {
        _paymentRepository = paymentRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result> Handle(VoidPaymentCommand request, CancellationToken cancellationToken)
    {
        var payment = await _paymentRepository.GetByIdAsync(request.PaymentId, cancellationToken);

        if (payment is null)
        {
            return Result.Failure(BillingErrors.PaymentNotFound);
        }

        var result = payment.Void(request.Reason);

        if (result.IsFailure)
        {
            return result;
        }

        _paymentRepository.Update(payment);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
