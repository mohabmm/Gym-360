using GymApp.Modules.Scheduling.Domain.Enums;

namespace GymApp.Modules.Scheduling.Application.Commands.CreateRecurringSessions;

/// <summary>What every occurrence of a recurring booking shares: its type, horizon and the trainer's buffer.</summary>
internal sealed record RecurringBooking(SessionType SessionType, int WeeksAhead, int BufferMinutes);
