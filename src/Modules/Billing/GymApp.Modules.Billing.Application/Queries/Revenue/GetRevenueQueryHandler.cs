using GymApp.Common.Application.Messaging;
using GymApp.Common.Domain;
using GymApp.Modules.Billing.Application.DTOs;
using GymApp.Modules.Billing.Domain.Repositories;

namespace GymApp.Modules.Billing.Application.Queries.Revenue;

internal sealed class GetRevenueQueryHandler : IQueryHandler<GetRevenueQuery, RevenueResponse>
{
    private readonly IPaymentRepository _paymentRepository;

    public GetRevenueQueryHandler(IPaymentRepository paymentRepository)
    {
        _paymentRepository = paymentRepository;
    }

    public async Task<Result<RevenueResponse>> Handle(GetRevenueQuery request, CancellationToken cancellationToken)
    {
        var from = request.From.ToDateTime(TimeOnly.MinValue);
        var to = request.To.ToDateTime(TimeOnly.MaxValue);

        var totalRevenue = await _paymentRepository.GetRevenueAsync(
            request.GymId, from, to, cancellationToken);

        var response = new RevenueResponse(
            totalRevenue,
            "EGP",
            request.From,
            request.To);

        return Result.Success(response);
    }
}
