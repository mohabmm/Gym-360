using GymApp.Modules.Progress.Application.Contracts;
using GymApp.Modules.Progress.Application.DTOs;
using GymApp.Modules.Progress.Domain.Enums;
using GymApp.Modules.Progress.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace GymApp.Modules.Progress.Infrastructure.Services;

internal sealed class ProgressQueryService : IProgressQueryService
{
    private readonly ProgressDbContext _dbContext;

    public ProgressQueryService(ProgressDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<BodyMetricDto?> GetLatestMetricsAsync(
        Guid userId,
        Guid gymId,
        CancellationToken cancellationToken = default)
    {
        var entry = await _dbContext.BodyMetricEntries
            .AsNoTracking()
            .Where(e => e.UserId == userId && e.GymId == gymId)
            .OrderByDescending(e => e.Date)
            .FirstOrDefaultAsync(cancellationToken);

        if (entry is null)
            return null;

        return new BodyMetricDto(
            entry.Id,
            entry.UserId,
            entry.GymId,
            entry.Date,
            entry.Weight,
            entry.WeightUnit.ToString(),
            entry.BodyFatPercentage,
            entry.Measurements is not null
                ? new BodyMeasurementsDto(
                    entry.Measurements.Chest,
                    entry.Measurements.Waist,
                    entry.Measurements.Hips,
                    entry.Measurements.LeftArm,
                    entry.Measurements.RightArm,
                    entry.Measurements.LeftThigh,
                    entry.Measurements.RightThigh,
                    entry.Measurements.LeftCalf,
                    entry.Measurements.RightCalf,
                    entry.Measurements.Neck,
                    entry.Measurements.MeasurementUnit.ToString())
                : null,
            entry.Source.ToString(),
            entry.LoggedBy,
            entry.Notes,
            entry.CreatedAt);
    }

    public async Task<decimal?> GetCurrentWeightAsync(
        Guid userId,
        Guid gymId,
        CancellationToken cancellationToken = default)
    {
        return await _dbContext.BodyMetricEntries
            .AsNoTracking()
            .Where(e => e.UserId == userId && e.GymId == gymId && e.Weight != null)
            .OrderByDescending(e => e.Date)
            .Select(e => e.Weight)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<int> GetProgressPhotoCountAsync(
        Guid userId,
        Guid gymId,
        CancellationToken cancellationToken = default)
    {
        return await _dbContext.ProgressPhotos
            .AsNoTracking()
            .CountAsync(e => e.UserId == userId && e.GymId == gymId, cancellationToken);
    }

    public async Task<int> GetWorkoutStreakAsync(
        Guid userId,
        Guid gymId,
        CancellationToken cancellationToken = default)
    {
        var streak = await _dbContext.Streaks
            .AsNoTracking()
            .FirstOrDefaultAsync(
                s => s.UserId == userId && s.GymId == gymId && s.StreakType == StreakType.DailyWorkout,
                cancellationToken);

        return streak?.CurrentStreak ?? 0;
    }
}
