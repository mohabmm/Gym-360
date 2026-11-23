namespace GymApp.Modules.Training.Domain.ValueObjects;

/// <summary>Calendar rules for plan counts. In Egypt the week starts on Saturday.</summary>
public static class PlanCalendar
{
    public static DateOnly WeekStart(DateOnly date) =>
        date.AddDays(-(((int)date.DayOfWeek - (int)DayOfWeek.Saturday + 7) % 7));

    public static DateOnly MonthStart(DateOnly date) => new(date.Year, date.Month, 1);
}
