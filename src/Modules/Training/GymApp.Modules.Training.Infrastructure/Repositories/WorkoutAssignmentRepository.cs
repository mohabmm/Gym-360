using GymApp.Modules.Training.Domain.Entities;
using GymApp.Modules.Training.Domain.Enums;
using GymApp.Modules.Training.Domain.Repositories;
using GymApp.Modules.Training.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace GymApp.Modules.Training.Infrastructure.Repositories;

internal sealed class WorkoutAssignmentRepository : IWorkoutAssignmentRepository
{
    private readonly TrainingDbContext _dbContext;

    public WorkoutAssignmentRepository(TrainingDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<WorkoutAssignment?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _dbContext.WorkoutAssignments
            .FirstOrDefaultAsync(wa => wa.Id == id, cancellationToken);
    }

    public async Task<IReadOnlyList<WorkoutAssignment>> GetByClientAsync(
        Guid clientId,
        Guid gymId,
        CancellationToken cancellationToken = default)
    {
        return await _dbContext.WorkoutAssignments
            .AsNoTracking()
            .Where(wa => wa.ClientId == clientId && wa.GymId == gymId)
            .OrderByDescending(wa => wa.StartDate)
            .ToListAsync(cancellationToken);
    }

    public async Task<WorkoutAssignment?> GetActiveByTrainerAndClientAsync(
        Guid trainerId,
        Guid clientId,
        Guid gymId,
        CancellationToken cancellationToken = default)
    {
        return await _dbContext.WorkoutAssignments
            .AsNoTracking()
            .Where(wa =>
                wa.TrainerId == trainerId &&
                wa.ClientId == clientId &&
                wa.GymId == gymId &&
                wa.Status == AssignmentStatus.Active)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task AddAsync(WorkoutAssignment assignment, CancellationToken cancellationToken = default)
    {
        await _dbContext.WorkoutAssignments.AddAsync(assignment, cancellationToken);
    }

    public void Update(WorkoutAssignment assignment)
    {
        _dbContext.WorkoutAssignments.Update(assignment);
    }
}
