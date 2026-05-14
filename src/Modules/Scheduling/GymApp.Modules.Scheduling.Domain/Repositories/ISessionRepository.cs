using GymApp.Modules.Scheduling.Domain.Entities;
using GymApp.Modules.Scheduling.Domain.Enums;

namespace GymApp.Modules.Scheduling.Domain.Repositories;

public interface ISessionRepository
{
    Task<Session?> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Session>> GetByTrainerAsync(
        Guid trainerId,
        Guid gymId,
        DateOnly startDate,
        DateOnly endDate,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Session>> GetByMemberAsync(
        Guid memberId,
        Guid gymId,
        DateOnly startDate,
        DateOnly endDate,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Session>> GetUpcomingByTrainerAsync(
        Guid trainerId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Session>> GetUpcomingByMemberAsync(
        Guid memberId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Session>> GetByDateAndTrainerAsync(
        Guid trainerId,
        DateOnly date,
        CancellationToken cancellationToken = default);

    Task AddAsync(
        Session session,
        CancellationToken cancellationToken = default);

    void Update(Session session);

    Task<int> GetCountByStatusAsync(
        Guid trainerId,
        Guid gymId,
        SessionStatus status,
        DateOnly startDate,
        DateOnly endDate,
        CancellationToken cancellationToken = default);
}
