using GymApp.Common.Application.Messaging;
using GymApp.Common.Domain;
using GymApp.Modules.Billing.Application.DTOs;
using GymApp.Modules.Billing.Domain.Enums;
using GymApp.Modules.Billing.Domain.Repositories;

namespace GymApp.Modules.Billing.Application.Queries.Revenue;

internal sealed class GetRevenueByTrainerQueryHandler : IQueryHandler<GetRevenueByTrainerQuery, List<TrainerRevenueDto>>
{
    private readonly IPaymentRepository _paymentRepository;

    public GetRevenueByTrainerQueryHandler(IPaymentRepository paymentRepository)
    {
        _paymentRepository = paymentRepository;
    }

    public async Task<Result<List<TrainerRevenueDto>>> Handle(GetRevenueByTrainerQuery request, CancellationToken cancellationToken)
    {
        var from = request.From.ToDateTime(TimeOnly.MinValue);
        var to = request.To.ToDateTime(TimeOnly.MaxValue);

        var payments = await _paymentRepository.GetByGymIdAsync(
            request.GymId, from, to, 1, int.MaxValue, cancellationToken);

        var trainerRevenue = payments
            .Where(p => p.Purpose == PaymentPurpose.SessionPackage && p.Status == PaymentStatus.Completed && p.ReferenceId.HasValue)
            .GroupBy(p => p.ReferenceId!.Value)
            .Select(g => new TrainerRevenueDto(
                g.Key,
                g.Count(),
                g.Sum(p => p.Amount.Amount),
                "EGP"))
            .ToList();

        return Result.Success(trainerRevenue);
    }
}
