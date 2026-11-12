namespace GymApp.Modules.Gyms.Shared;

/// <summary>
/// The gym's front-desk audience for PT alerts (trial ending, last session, disputes):
/// every staff member with a gym role (owner, manager, receptionist).
/// </summary>
public interface IGymDeskLookup
{
    Task<IReadOnlyList<Guid>> GetDeskUserIdsAsync(Guid gymId, CancellationToken cancellationToken = default);
}
