using GymApp.Common.Application.Messaging;
using GymApp.Common.Domain;
using GymApp.Modules.Nutrition.Application.DTOs;
using GymApp.Modules.Nutrition.Application.Mappers;
using GymApp.Modules.Nutrition.Domain.Repositories;

namespace GymApp.Modules.Nutrition.Application.Queries.GetWaterLog;

internal sealed class GetWaterLogQueryHandler : IQueryHandler<GetWaterLogQuery, WaterLogResponse>
{
    // Matches the per-day default applied when a new WaterLog is created.
    private const int DefaultDailyTargetMl = 2000;

    private readonly IWaterLogRepository _waterLogRepository;

    public GetWaterLogQueryHandler(IWaterLogRepository waterLogRepository)
    {
        _waterLogRepository = waterLogRepository;
    }

    public async Task<Result<WaterLogResponse>> Handle(GetWaterLogQuery request, CancellationToken cancellationToken)
    {
        var waterLog = await _waterLogRepository.GetByUserAndDateAsync(
            request.UserId, request.GymId, request.Date, cancellationToken);

        // No log yet for this day is the expected state for a fresh member —
        // return an empty response so the mobile home screen can render
        // "0 / 2000 ml" instead of surfacing a 400 error to the user.
        if (waterLog is null)
        {
            return Result.Success(new WaterLogResponse(
                Guid.Empty,
                request.UserId,
                request.Date,
                TotalMl: 0,
                TargetMl: DefaultDailyTargetMl,
                Entries: new List<WaterEntryDto>()));
        }

        return Result.Success(NutritionMapper.ToWaterLogResponse(waterLog));
    }
}
