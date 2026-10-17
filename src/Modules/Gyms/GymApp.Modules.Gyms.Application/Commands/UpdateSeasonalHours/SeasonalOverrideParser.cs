using GymApp.Common.Domain;
using GymApp.Modules.Gyms.Application.DTOs;
using GymApp.Modules.Gyms.Domain.ValueObjects;

namespace GymApp.Modules.Gyms.Application.Commands.UpdateSeasonalHours;

/// <summary>Turns the raw string input of a seasonal override into validated domain values.</summary>
internal static class SeasonalOverrideParser
{
    public static Result<SeasonalHoursDetails> Parse(SeasonalOverrideInput input)
    {
        if (!DateOnly.TryParse(input.StartDate, out var start))
            return Result.Failure<SeasonalHoursDetails>(new Error("Gyms.InvalidDate", $"Invalid start date: {input.StartDate}"));
        if (!DateOnly.TryParse(input.EndDate, out var end))
            return Result.Failure<SeasonalHoursDetails>(new Error("Gyms.InvalidDate", $"Invalid end date: {input.EndDate}"));
        if (end < start)
            return Result.Failure<SeasonalHoursDetails>(new Error("Gyms.InvalidDateRange", "End date must be on or after start date."));

        var schedules = ParseDaySchedules(input.DaySchedules ?? []);
        if (schedules.IsFailure)
            return Result.Failure<SeasonalHoursDetails>(schedules.Error);

        return Result.Success(new SeasonalHoursDetails
        {
            Name = input.Name,
            StartDate = start,
            EndDate = end,
            DaySchedules = schedules.Value
        });
    }

    private static Result<List<DaySchedule>> ParseDaySchedules(List<DayScheduleDto> inputs)
    {
        var schedules = new List<DaySchedule>();
        foreach (var ds in inputs)
        {
            var schedule = ParseDaySchedule(ds);
            if (schedule.IsFailure)
                return Result.Failure<List<DaySchedule>>(schedule.Error);
            schedules.Add(schedule.Value);
        }

        return Result.Success(schedules);
    }

    private static Result<DaySchedule> ParseDaySchedule(DayScheduleDto ds)
    {
        if (!Enum.TryParse<DayOfWeek>(ds.DayOfWeek, ignoreCase: true, out var day))
            return Result.Failure<DaySchedule>(new Error("Gyms.InvalidDayOfWeek", $"Invalid day: {ds.DayOfWeek}"));
        if (!TimeOnly.TryParse(ds.OpenTime, out var open))
            return Result.Failure<DaySchedule>(new Error("Gyms.InvalidTime", $"Invalid open: {ds.OpenTime}"));
        if (!TimeOnly.TryParse(ds.CloseTime, out var close))
            return Result.Failure<DaySchedule>(new Error("Gyms.InvalidTime", $"Invalid close: {ds.CloseTime}"));

        return Result.Success(new DaySchedule(day, open, close, ds.IsClosed));
    }
}
