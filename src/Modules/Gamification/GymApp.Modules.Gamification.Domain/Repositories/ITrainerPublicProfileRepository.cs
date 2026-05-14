using GymApp.Modules.Gamification.Domain.Entities;

namespace GymApp.Modules.Gamification.Domain.Repositories;

public interface ITrainerPublicProfileRepository
{
    Task<TrainerPublicProfile?> GetByTrainerAndGymAsync(
        Guid trainerId,
        Guid gymId,
        CancellationToken cancellationToken = default);

    Task<TrainerPublicProfile?> FindByTestimonialIdAsync(
        Guid testimonialId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<TrainerPublicProfile>> ListWithPendingTestimonialsByGymAsync(
        Guid gymId,
        CancellationToken cancellationToken = default);

    Task AddAsync(
        TrainerPublicProfile profile,
        CancellationToken cancellationToken = default);

    void Update(TrainerPublicProfile profile);
}
