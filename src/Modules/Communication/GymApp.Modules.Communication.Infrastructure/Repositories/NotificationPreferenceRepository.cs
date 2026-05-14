using GymApp.Modules.Communication.Domain.Entities;
using GymApp.Modules.Communication.Domain.Repositories;
using GymApp.Modules.Communication.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace GymApp.Modules.Communication.Infrastructure.Repositories;

internal sealed class NotificationPreferenceRepository : INotificationPreferenceRepository
{
    private readonly CommunicationDbContext _dbContext;

    public NotificationPreferenceRepository(CommunicationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<NotificationPreference?> GetByUserAsync(
        Guid userId,
        Guid? gymId = null,
        CancellationToken cancellationToken = default)
    {
        return await _dbContext.NotificationPreferences
            .FirstOrDefaultAsync(
                np => np.UserId == userId && np.GymId == gymId,
                cancellationToken);
    }

    public async Task AddAsync(NotificationPreference preference, CancellationToken cancellationToken = default)
    {
        await _dbContext.NotificationPreferences.AddAsync(preference, cancellationToken);
    }

    public void Update(NotificationPreference preference)
    {
        _dbContext.NotificationPreferences.Update(preference);
    }
}
