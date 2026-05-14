using GymApp.Common.Application.Messaging;
using GymApp.Common.Domain;
using GymApp.Modules.Billing.Application.DTOs;
using GymApp.Modules.Billing.Domain.Enums;
using GymApp.Modules.Billing.Domain.Repositories;

namespace GymApp.Modules.Billing.Application.Queries.MemberBalance;

internal sealed class GetMemberBalanceQueryHandler : IQueryHandler<GetMemberBalanceQuery, MemberBalanceResponse>
{
    private readonly IPaymentRepository _paymentRepository;
    private readonly ISessionPackageRepository _packageRepository;

    public GetMemberBalanceQueryHandler(
        IPaymentRepository paymentRepository,
        ISessionPackageRepository packageRepository)
    {
        _paymentRepository = paymentRepository;
        _packageRepository = packageRepository;
    }

    public async Task<Result<MemberBalanceResponse>> Handle(GetMemberBalanceQuery request, CancellationToken cancellationToken)
    {
        var payments = await _paymentRepository.GetByMemberAsync(
            request.GymId, request.MemberId, cancellationToken);

        var totalPaid = payments
            .Where(p => p.Status == PaymentStatus.Completed)
            .Sum(p => p.Amount.Amount);

        var totalVoided = payments
            .Where(p => p.Status == PaymentStatus.Voided)
            .Sum(p => p.Amount.Amount);

        var packages = await _packageRepository.GetByMemberAsync(
            request.GymId, request.MemberId, cancellationToken);

        var activePackages = packages
            .Where(p => p.Status == PackageStatus.Active)
            .ToList();

        var totalRemainingSessions = activePackages.Sum(p => p.RemainingSessions);

        var response = new MemberBalanceResponse(
            request.GymId,
            request.MemberId,
            totalPaid,
            totalVoided,
            totalPaid - totalVoided,
            "EGP",
            activePackages.Count,
            totalRemainingSessions);

        return Result.Success(response);
    }
}
