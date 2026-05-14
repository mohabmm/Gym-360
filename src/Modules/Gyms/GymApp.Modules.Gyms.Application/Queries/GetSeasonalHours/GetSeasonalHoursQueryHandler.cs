using GymApp.Common.Application.Messaging;
using GymApp.Common.Domain;
using GymApp.Modules.Gyms.Application.DTOs;
using GymApp.Modules.Gyms.Domain.Repositories;

namespace GymApp.Modules.Gyms.Application.Queries.GetSeasonalHours;

internal sealed class GetSeasonalHoursQueryHandler : IQueryHandler<GetSeasonalHoursQuery, List<GymSeasonalHoursDto>>
{
    private readonly IGymSeasonalHoursRepository _seasonalRepository;

    public GetSeasonalHoursQueryHandler(IGymSeasonalHoursRepository seasonalRepository)
    {
        _seasonalRepository = seasonalRepository;
    }

    public async Task<Result<List<GymSeasonalHoursDto>>> Handle(GetSeasonalHoursQuery request, CancellationToken cancellationToken)
    {
        var items = await _seasonalRepository.GetByGymIdAsync(request.GymId, cancellationToken);
        var dtos = items
            .Select(h => new GymSeasonalHoursDto(
                h.Id,
                h.Name,
                h.StartDate.ToString("yyyy-MM-dd"),
                h.EndDate.ToString("yyyy-MM-dd"),
                h.IsActive,
                h.DaySchedules.Select(ds => new DayScheduleDto(
                    ds.DayOfWeek.ToString(),
                    ds.OpenTime.ToString("HH:mm"),
                    ds.CloseTime.ToString("HH:mm"),
                    ds.IsClosed)).ToList()))
            .ToList();
        return Result.Success(dtos);
    }
}
