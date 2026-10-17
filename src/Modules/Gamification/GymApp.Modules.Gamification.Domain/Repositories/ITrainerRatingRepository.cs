using GymApp.Modules.Gamification.Domain.Entities;
using GymApp.Modules.Gamification.Domain.ValueObjects;

namespace GymApp.Modules.Gamification.Domain.Repositories;

public interface ITrainerRatingRepository
{
    Task<TrainerRating?> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<TrainerRating>> GetByTrainerAsync(
        Guid gymId,
        Guid trainerId,
        DateTimeRange range,
        CancellationToken cancellationToken = default);

    Task<TrainerRating?> GetBySessionAsync(
        Guid sessionId,
        CancellationToken cancellationToken = default);

    Task<decimal> GetAverageAsync(
        Guid trainerId,
        Guid gymId,
        CancellationToken cancellationToken = default);

    Task AddAsync(
        TrainerRating rating,
        CancellationToken cancellationToken = default);
}
