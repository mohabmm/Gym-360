namespace GymApp.Common.Application.Abstractions;

public interface IGymLookupService
{
    Task<string?> GetGymNameAsync(Guid gymId, CancellationToken cancellationToken = default);
}
