using GymApp.Common.Application.Messaging;
using GymApp.Common.Domain;
using GymApp.Modules.Billing.Application.DTOs;
using GymApp.Modules.Billing.Domain.Repositories;

namespace GymApp.Modules.Billing.Application.Queries.GetTrainerEarnings;

internal sealed class GetTrainerEarningsQueryHandler : IQueryHandler<GetTrainerEarningsQuery, TrainerEarningsSummaryDto>
{
    private readonly ITrainerEarningsRepository _earningsRepository;

    public GetTrainerEarningsQueryHandler(ITrainerEarningsRepository earningsRepository)
    {
        _earningsRepository = earningsRepository;
    }

    public async Task<Result<TrainerEarningsSummaryDto>> Handle(
        GetTrainerEarningsQuery request,
        CancellationToken cancellationToken)
    {
        var from = request.From.HasValue
            ? request.From.Value.ToDateTime(TimeOnly.MinValue)
            : new DateTime(DateTime.UtcNow.Year, DateTime.UtcNow.Month, 1);

        var to = request.To.HasValue
            ? request.To.Value.ToDateTime(TimeOnly.MaxValue)
            : DateTime.UtcNow;

        var records = await _earningsRepository.GetByTrainerAsync(
            request.GymId,
            request.TrainerId,
            from,
            to,
            cancellationToken);

        var recordDtos = records
            .Select(r => new EarningsRecordDto(r.SessionId, r.MemberId, r.RateApplied, r.Currency, r.EarnedAt))
            .ToList();

        var summary = new TrainerEarningsSummaryDto(
            request.GymId,
            request.TrainerId,
            TotalSessions: records.Count,
            TotalEarningsEgp: records.Sum(r => r.RateApplied),
            Currency: "EGP",
            PeriodFrom: from,
            PeriodTo: to,
            Records: recordDtos);

        return Result.Success(summary);
    }
}
