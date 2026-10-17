using GymApp.Modules.Scheduling.Domain.Entities;
using GymApp.Modules.Scheduling.Domain.Enums;
using GymApp.Modules.Scheduling.Domain.ValueObjects;

namespace GymApp.Modules.Scheduling.Domain.Repositories;

public interface ISessionRepository
{
    Task<Session?> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Session>> GetByTrainerAsync(
        Guid trainerId,
        GymDateRange range,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Session>> GetByMemberAsync(
        Guid memberId,
        GymDateRange range,
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
        SessionStatus status,
        GymDateRange range,
        CancellationToken cancellationToken = default);
}
