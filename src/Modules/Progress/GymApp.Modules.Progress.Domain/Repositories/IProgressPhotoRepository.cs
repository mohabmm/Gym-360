using GymApp.Modules.Progress.Domain.Entities;

namespace GymApp.Modules.Progress.Domain.Repositories;

public interface IProgressPhotoRepository
{
    Task<ProgressPhoto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<ProgressPhoto>> GetByUserAsync(
        Guid userId,
        Guid gymId,
        PageRequest page,
        CancellationToken cancellationToken = default);

    Task<int> GetCountAsync(
        Guid userId,
        Guid gymId,
        CancellationToken cancellationToken = default);

    Task AddAsync(ProgressPhoto photo, CancellationToken cancellationToken = default);

    void Delete(ProgressPhoto photo);
}
