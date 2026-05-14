using GymApp.Common.Application.Messaging;
using GymApp.Common.Domain;
using GymApp.Modules.Gyms.Application.Abstractions;
using GymApp.Modules.Gyms.Domain.Entities;
using GymApp.Modules.Gyms.Domain.Errors;
using GymApp.Modules.Gyms.Domain.Repositories;
using GymApp.Modules.Gyms.Domain.ValueObjects;

namespace GymApp.Modules.Gyms.Application.Commands.UpdateSeasonalHours;

internal sealed class UpdateSeasonalHoursCommandHandler : ICommandHandler<UpdateSeasonalHoursCommand>
{
    private readonly IGymRepository _gymRepository;
    private readonly IGymSeasonalHoursRepository _seasonalRepository;
    private readonly IGymsUnitOfWork _unitOfWork;

    public UpdateSeasonalHoursCommandHandler(
        IGymRepository gymRepository,
        IGymSeasonalHoursRepository seasonalRepository,
        IGymsUnitOfWork unitOfWork)
    {
        _gymRepository = gymRepository;
        _seasonalRepository = seasonalRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result> Handle(UpdateSeasonalHoursCommand request, CancellationToken cancellationToken)
    {
        var gym = await _gymRepository.GetByIdAsync(request.GymId, cancellationToken);
        if (gym is null)
            return Result.Failure(GymErrors.NotFound);

        var existing = await _seasonalRepository.GetByGymIdAsync(request.GymId, cancellationToken);
        foreach (var item in existing)
            _seasonalRepository.Remove(item);

        foreach (var input in request.Overrides)
        {
            if (!DateOnly.TryParse(input.StartDate, out var start))
                return Result.Failure(new Error("Gyms.InvalidDate", $"Invalid start date: {input.StartDate}"));
            if (!DateOnly.TryParse(input.EndDate, out var end))
                return Result.Failure(new Error("Gyms.InvalidDate", $"Invalid end date: {input.EndDate}"));
            if (end < start)
                return Result.Failure(new Error("Gyms.InvalidDateRange", "End date must be on or after start date."));

            var schedules = new List<DaySchedule>();
            if (input.DaySchedules is not null)
            {
                foreach (var ds in input.DaySchedules)
                {
                    if (!Enum.TryParse<DayOfWeek>(ds.DayOfWeek, ignoreCase: true, out var day))
                        return Result.Failure(new Error("Gyms.InvalidDayOfWeek", $"Invalid day: {ds.DayOfWeek}"));
                    if (!TimeOnly.TryParse(ds.OpenTime, out var open))
                        return Result.Failure(new Error("Gyms.InvalidTime", $"Invalid open: {ds.OpenTime}"));
                    if (!TimeOnly.TryParse(ds.CloseTime, out var close))
                        return Result.Failure(new Error("Gyms.InvalidTime", $"Invalid close: {ds.CloseTime}"));
                    schedules.Add(new DaySchedule(day, open, close, ds.IsClosed));
                }
            }

            var entity = GymSeasonalHours.Create(request.GymId, input.Name, start, end, schedules);
            if (!input.IsActive) entity.Deactivate();
            await _seasonalRepository.AddAsync(entity, cancellationToken);
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}
