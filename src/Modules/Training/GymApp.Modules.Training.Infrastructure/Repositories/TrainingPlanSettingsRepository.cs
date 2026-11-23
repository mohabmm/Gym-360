using GymApp.Modules.Training.Domain.Entities;
using GymApp.Modules.Training.Domain.Repositories;
using GymApp.Modules.Training.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace GymApp.Modules.Training.Infrastructure.Repositories;

internal sealed class TrainingPlanSettingsRepository : ITrainingPlanSettingsRepository
{
    private readonly TrainingDbContext _db;

    public TrainingPlanSettingsRepository(TrainingDbContext db) => _db = db;

    public Task<TrainingPlanSettings?> GetAsync(Guid gymId, CancellationToken cancellationToken = default) =>
        _db.TrainingPlanSettings.FirstOrDefaultAsync(s => s.GymId == gymId, cancellationToken);

    public async Task AddAsync(TrainingPlanSettings settings, CancellationToken cancellationToken = default) =>
        await _db.TrainingPlanSettings.AddAsync(settings, cancellationToken);
}
