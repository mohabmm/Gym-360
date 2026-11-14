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

    /// <summary>pt.exercise_progress: the member's completed sessions in a gym that have exercise logs, oldest first, read-only.</summary>
    Task<IReadOnlyList<Session>> GetCompletedWithExerciseLogsByMemberAsync(
        MemberInGym member,
        CancellationToken cancellationToken = default);

    /// <summary>True when the trainer has at least one session with the member in this gym.</summary>
    Task<bool> HasSessionWithMemberAsync(
        SessionParticipants participants,
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

    /// <summary>A quick-logged session for the same coach and client since <paramref name="sinceUtc"/> (double-tap guard).</summary>
    Task<Session?> FindRecentQuickLogAsync(
        Guid trainerId,
        Guid memberId,
        DateTime sinceUtc,
        CancellationToken cancellationToken = default);

    /// <summary>Sessions in the gym with the given trainee-confirmation state (desk review list).</summary>
    Task<IReadOnlyList<Session>> GetByMemberConfirmationAsync(
        Guid gymId,
        MemberConfirmationStatus status,
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
