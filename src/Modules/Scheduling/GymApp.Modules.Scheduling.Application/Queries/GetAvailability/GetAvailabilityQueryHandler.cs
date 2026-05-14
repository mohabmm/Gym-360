using GymApp.Common.Application.Messaging;
using GymApp.Common.Domain;
using GymApp.Modules.Scheduling.Application.DTOs;
using GymApp.Modules.Scheduling.Domain.Repositories;

namespace GymApp.Modules.Scheduling.Application.Queries.GetAvailability;

internal sealed class GetAvailabilityQueryHandler
    : IQueryHandler<GetAvailabilityQuery, AvailabilityResponse>
{
    private readonly ITrainerAvailabilityRepository _availabilityRepository;

    public GetAvailabilityQueryHandler(ITrainerAvailabilityRepository availabilityRepository)
    {
        _availabilityRepository = availabilityRepository;
    }

    public async Task<Result<AvailabilityResponse>> Handle(
        GetAvailabilityQuery request,
        CancellationToken cancellationToken)
    {
        var availability = await _availabilityRepository.GetByTrainerAndGymAsync(
            request.TrainerId, request.GymId, cancellationToken);

        if (availability is null)
        {
            return Result.Success(new AvailabilityResponse(
                request.TrainerId,
                request.GymId,
                SessionDurationMinutes: 60,
                BufferMinutes: 15,
                WeeklySlots: new List<AvailabilitySlotDto>(),
                BlockedDates: new List<BlockedDateDto>(),
                UpdatedAt: DateTime.UtcNow));
        }

        var response = new AvailabilityResponse(
            availability.TrainerId,
            availability.GymId,
            availability.SessionDurationMinutes,
            availability.BufferMinutes,
            availability.WeeklySlots
                .Where(s => s.IsActive)
                .Select(s => new AvailabilitySlotDto(s.DayOfWeek, s.StartTime, s.EndTime))
                .ToList(),
            availability.BlockedDates
                .Select(b => new BlockedDateDto(b.Date, b.Reason, b.AutoNotifyClients))
                .ToList(),
            availability.UpdatedAt);

        return Result.Success(response);
    }
}
