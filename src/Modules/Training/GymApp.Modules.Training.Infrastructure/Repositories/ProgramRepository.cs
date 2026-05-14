using GymApp.Modules.Training.Domain.Entities;
using GymApp.Modules.Training.Domain.Repositories;
using GymApp.Modules.Training.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace GymApp.Modules.Training.Infrastructure.Repositories;

internal sealed class ProgramRepository : IProgramRepository
{
    private readonly TrainingDbContext _dbContext;

    public ProgramRepository(TrainingDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<Program?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _dbContext.Programs
            .Include(p => p.ProgramWeeks.OrderBy(pw => pw.WeekNumber))
            .FirstOrDefaultAsync(p => p.Id == id, cancellationToken);
    }

    public async Task<IReadOnlyList<Program>> GetByTrainerAndGymAsync(
        Guid trainerId,
        Guid gymId,
        CancellationToken cancellationToken = default)
    {
        return await _dbContext.Programs
            .AsNoTracking()
            .Include(p => p.ProgramWeeks.OrderBy(pw => pw.WeekNumber))
            .Where(p => p.TrainerId == trainerId && p.GymId == gymId)
            .OrderByDescending(p => p.UpdatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task AddAsync(Program program, CancellationToken cancellationToken = default)
    {
        await _dbContext.Programs.AddAsync(program, cancellationToken);
    }

    public void Update(Program program)
    {
        _dbContext.Programs.Update(program);
    }
}
