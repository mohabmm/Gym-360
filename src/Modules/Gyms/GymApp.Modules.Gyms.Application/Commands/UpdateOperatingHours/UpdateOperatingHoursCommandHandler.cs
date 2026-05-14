using GymApp.Common.Application.Messaging;
using GymApp.Common.Domain;
using GymApp.Modules.Gyms.Application.Abstractions;
using GymApp.Modules.Gyms.Domain.Errors;
using GymApp.Modules.Gyms.Domain.Repositories;
using GymApp.Modules.Gyms.Domain.ValueObjects;

namespace GymApp.Modules.Gyms.Application.Commands.UpdateOperatingHours;

internal sealed class UpdateOperatingHoursCommandHandler : ICommandHandler<UpdateOperatingHoursCommand>
{
    private readonly IGymRepository _gymRepository;
    private readonly IGymsUnitOfWork _unitOfWork;

    public UpdateOperatingHoursCommandHandler(IGymRepository gymRepository, IGymsUnitOfWork unitOfWork)
    {
        _gymRepository = gymRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result> Handle(UpdateOperatingHoursCommand request, CancellationToken cancellationToken)
    {
        var gym = await _gymRepository.GetByIdAsync(request.GymId, cancellationToken);
        if (gym is null)
            return Result.Failure(GymErrors.NotFound);

        var schedules = new List<DaySchedule>();
        foreach (var dto in request.Hours)
        {
            if (!Enum.TryParse<DayOfWeek>(dto.DayOfWeek, ignoreCase: true, out var day))
                return Result.Failure(new Error("Gyms.InvalidDayOfWeek", $"Invalid day of week: {dto.DayOfWeek}"));

            if (!TimeOnly.TryParse(dto.OpenTime, out var open))
                return Result.Failure(new Error("Gyms.InvalidTime", $"Invalid open time: {dto.OpenTime}"));

            if (!TimeOnly.TryParse(dto.CloseTime, out var close))
                return Result.Failure(new Error("Gyms.InvalidTime", $"Invalid close time: {dto.CloseTime}"));

            schedules.Add(new DaySchedule(day, open, close, dto.IsClosed));
        }

        gym.SetOperatingHours(schedules);
        _gymRepository.Update(gym);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
