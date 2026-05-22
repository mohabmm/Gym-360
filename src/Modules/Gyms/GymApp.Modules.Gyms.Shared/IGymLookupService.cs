namespace GymApp.Modules.Gyms.Shared;

public interface IGymLookupService
{
    Task<string?> GetGymNameAsync(Guid gymId, CancellationToken cancellationToken = default);
}
