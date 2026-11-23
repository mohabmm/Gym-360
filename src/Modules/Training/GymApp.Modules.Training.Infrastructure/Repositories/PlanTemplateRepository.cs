using GymApp.Modules.Training.Domain.Entities;
using GymApp.Modules.Training.Domain.Enums;
using GymApp.Modules.Training.Domain.Repositories;
using GymApp.Modules.Training.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace GymApp.Modules.Training.Infrastructure.Repositories;

internal sealed class PlanTemplateRepository : IPlanTemplateRepository
{
    private readonly TrainingDbContext _db;

    public PlanTemplateRepository(TrainingDbContext db) => _db = db;

    public Task<PlanTemplate?> GetByIdAsync(Guid gymId, Guid templateId, CancellationToken cancellationToken = default) =>
        _db.PlanTemplates.FirstOrDefaultAsync(t => t.Id == templateId && t.GymId == gymId, cancellationToken);

    public async Task<IReadOnlyList<PlanTemplate>> ListVisibleAsync(Guid gymId, Guid? trainerId, CancellationToken cancellationToken = default) =>
        await _db.PlanTemplates
            .AsNoTracking()
            .Where(t => t.GymId == gymId && !t.IsArchived)
            .Where(t => t.Scope == PlanTemplateScope.Gym || (trainerId != null && t.OwnerTrainerId == trainerId))
            .OrderBy(t => t.Scope)
            .ThenByDescending(t => t.UpdatedAt)
            .ToListAsync(cancellationToken);

    public Task<bool> AnyForGymAsync(Guid gymId, CancellationToken cancellationToken = default) =>
        _db.PlanTemplates.AsNoTracking().AnyAsync(t => t.GymId == gymId && t.Scope == PlanTemplateScope.Gym, cancellationToken);

    public async Task AddAsync(PlanTemplate template, CancellationToken cancellationToken = default) =>
        await _db.PlanTemplates.AddAsync(template, cancellationToken);
}
