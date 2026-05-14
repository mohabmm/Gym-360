namespace GymApp.Modules.Gyms.Application.Abstractions;

public interface IImportJobScheduler
{
    Task EnqueueAsync(Guid jobId, CancellationToken cancellationToken = default);
}
