using GymApp.Common.Application.Messaging;
using GymApp.Common.Domain;
using GymApp.Modules.Gyms.Application.DTOs;
using GymApp.Modules.Gyms.Domain.Errors;
using GymApp.Modules.Gyms.Domain.Repositories;

namespace GymApp.Modules.Gyms.Application.Queries.GetOperatingHours;

internal sealed class GetOperatingHoursQueryHandler : IQueryHandler<GetOperatingHoursQuery, List<DayScheduleDto>>
{
    private readonly IGymRepository _gymRepository;

    public GetOperatingHoursQueryHandler(IGymRepository gymRepository)
    {
        _gymRepository = gymRepository;
    }

    public async Task<Result<List<DayScheduleDto>>> Handle(GetOperatingHoursQuery request, CancellationToken cancellationToken)
    {
        var gym = await _gymRepository.GetByIdAsync(request.GymId, cancellationToken);
        if (gym is null)
            return Result.Failure<List<DayScheduleDto>>(GymErrors.NotFound);

        var dtos = gym.OperatingHours
            .Select(h => new DayScheduleDto(
                h.DayOfWeek.ToString(),
                h.OpenTime.ToString("HH:mm"),
                h.CloseTime.ToString("HH:mm"),
                h.IsClosed))
            .ToList();

        return Result.Success(dtos);
    }
}
