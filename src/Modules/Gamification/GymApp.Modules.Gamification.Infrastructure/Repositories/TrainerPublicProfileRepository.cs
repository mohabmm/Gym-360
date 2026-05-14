using GymApp.Modules.Gamification.Domain.Entities;
using GymApp.Modules.Gamification.Domain.Repositories;
using GymApp.Modules.Gamification.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace GymApp.Modules.Gamification.Infrastructure.Repositories;

internal sealed class TrainerPublicProfileRepository : ITrainerPublicProfileRepository
{
    private readonly GamificationDbContext _dbContext;

    public TrainerPublicProfileRepository(GamificationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<TrainerPublicProfile?> GetByTrainerAndGymAsync(
        Guid trainerId,
        Guid gymId,
        CancellationToken cancellationToken = default)
    {
        // Load all testimonials (approved + pending + rejected) so the moderation
        // command can find pending ones. The public-profile query handler filters
        // to approved-only when building its DTO.
        return await _dbContext.TrainerPublicProfiles
            .Include(tpp => tpp.Testimonials.OrderByDescending(t => t.CreatedAt))
            .FirstOrDefaultAsync(
                tpp => tpp.TrainerId == trainerId && tpp.GymId == gymId,
                cancellationToken);
    }

    public async Task<TrainerPublicProfile?> FindByTestimonialIdAsync(
        Guid testimonialId,
        CancellationToken cancellationToken = default)
    {
        return await _dbContext.TrainerPublicProfiles
            .Include(tpp => tpp.Testimonials)
            .FirstOrDefaultAsync(
                tpp => tpp.Testimonials.Any(t => t.Id == testimonialId),
                cancellationToken);
    }

    public async Task<IReadOnlyList<TrainerPublicProfile>> ListWithPendingTestimonialsByGymAsync(
        Guid gymId,
        CancellationToken cancellationToken = default)
    {
        return await _dbContext.TrainerPublicProfiles
            .AsNoTracking()
            .Include(tpp => tpp.Testimonials
                .Where(t => !t.IsApproved && t.ModeratedAt == null)
                .OrderByDescending(t => t.CreatedAt))
            .Where(tpp => tpp.GymId == gymId
                && tpp.Testimonials.Any(t => !t.IsApproved && t.ModeratedAt == null))
            .ToListAsync(cancellationToken);
    }

    public async Task AddAsync(TrainerPublicProfile profile, CancellationToken cancellationToken = default)
    {
        await _dbContext.TrainerPublicProfiles.AddAsync(profile, cancellationToken);
    }

    public void Update(TrainerPublicProfile profile)
    {
        _dbContext.TrainerPublicProfiles.Update(profile);
    }
}
