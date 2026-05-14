using GymApp.Common.Application.Abstractions;

namespace GymApp.Common.Infrastructure.Clock;

public sealed class DateTimeProvider : IDateTimeProvider
{
    public DateTime UtcNow => DateTime.UtcNow;
}
