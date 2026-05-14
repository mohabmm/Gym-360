using GymApp.Common.Application.Abstractions;
using GymApp.Modules.Scheduling.Domain.Repositories;

namespace GymApp.Modules.Scheduling.Application.Services;

internal sealed class UserDataProvider : IUserDataProvider
{
    private readonly ISessionRepository _sessionRepository;

    public UserDataProvider(ISessionRepository sessionRepository)
    {
        _sessionRepository = sessionRepository;
    }

    public string ModuleName => "Scheduling";

    public async Task<object> GetUserDataAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var upcomingSessions = await _sessionRepository.GetUpcomingByMemberAsync(userId, cancellationToken);
        var upcomingAsTrainer = await _sessionRepository.GetUpcomingByTrainerAsync(userId, cancellationToken);

        return new
        {
            UpcomingSessionsAsMember = upcomingSessions.Select(s => new
            {
                s.Id,
                s.GymId,
                s.TrainerId,
                s.ScheduledDate,
                s.ScheduledTime,
                s.DurationMinutes,
                s.SessionType,
                s.Status
            }),
            UpcomingSessionsAsTrainer = upcomingAsTrainer.Select(s => new
            {
                s.Id,
                s.GymId,
                s.MemberId,
                s.ScheduledDate,
                s.ScheduledTime,
                s.DurationMinutes,
                s.SessionType,
                s.Status
            })
        };
    }
}
