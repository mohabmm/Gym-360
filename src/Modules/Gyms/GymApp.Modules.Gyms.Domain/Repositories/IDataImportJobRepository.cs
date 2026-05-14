using GymApp.Modules.Gyms.Domain.Entities;

namespace GymApp.Modules.Gyms.Domain.Repositories;

public interface IDataImportJobRepository
{
    Task<DataImportJob?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<DataImportJob>> ListByGymAsync(Guid gymId, int skip, int take, CancellationToken cancellationToken = default);
    Task AddAsync(DataImportJob job, CancellationToken cancellationToken = default);
    void Update(DataImportJob job);
}
