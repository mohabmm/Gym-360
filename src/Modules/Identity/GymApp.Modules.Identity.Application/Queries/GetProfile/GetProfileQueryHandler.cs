using GymApp.Modules.Identity.Shared;
using GymApp.Modules.Billing.Shared;
using GymApp.Common.Application.Messaging;
using GymApp.Common.Application.Tenancy;
using GymApp.Common.Domain;
using GymApp.Modules.Identity.Application.DTOs;
using GymApp.Modules.Identity.Domain.Enums;
using GymApp.Modules.Identity.Domain.Errors;
using GymApp.Modules.Identity.Domain.Repositories;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace GymApp.Modules.Identity.Application.Queries.GetProfile;

internal sealed class GetProfileQueryHandler : IQueryHandler<GetProfileQuery, UserProfileResponse>
{
    private readonly IUserRepository _userRepository;
    private readonly ITrainerAssignmentLookup _trainerAssignmentLookup;
    private readonly IUserDisplayLookup _userDisplayLookup;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IGymRegistry _gymRegistry;
    private readonly ITenantContextAccessor _tenantAccessor;
    private readonly ILogger<GetProfileQueryHandler> _logger;

    public GetProfileQueryHandler(
        IUserRepository userRepository,
        ITrainerAssignmentLookup trainerAssignmentLookup,
        IUserDisplayLookup userDisplayLookup,
        IServiceScopeFactory scopeFactory,
        IGymRegistry gymRegistry,
        ITenantContextAccessor tenantAccessor,
        ILogger<GetProfileQueryHandler> logger)
    {
        _userRepository = userRepository;
        _trainerAssignmentLookup = trainerAssignmentLookup;
        _userDisplayLookup = userDisplayLookup;
        _scopeFactory = scopeFactory;
        _gymRegistry = gymRegistry;
        _tenantAccessor = tenantAccessor;
        _logger = logger;
    }

    public async Task<Result<UserProfileResponse>> Handle(GetProfileQuery request, CancellationToken cancellationToken)
    {
        var user = await _userRepository.GetByIdAsync(request.UserId, cancellationToken);
        if (user is null)
            return Result.Failure<UserProfileResponse>(UserErrors.NotFound(request.UserId));

        // Trainers-by-gym resolved only for trainee memberships — owners/staff/trainers
        // don't have a "my assigned trainer" concept in the UI.
        var traineeGymIds = user.GymMemberships
            .Where(m => m.IsActive && m.Role == UserRole.Trainee)
            .Select(m => m.GymId)
            .ToList();

        var trainersByGym = traineeGymIds.Count > 0
            ? await ResolveTrainersAcrossGymsAsync(user.Id, traineeGymIds, cancellationToken)
            : new Dictionary<Guid, Guid>();

        var trainerDisplays = trainersByGym.Count > 0
            ? await _userDisplayLookup.GetDisplayNamesAsync(trainersByGym.Values.Distinct(), cancellationToken)
            : new Dictionary<Guid, UserDisplay>();

        var gymMemberships = user.GymMemberships
            .Select(m =>
            {
                Guid? trainerId = trainersByGym.TryGetValue(m.GymId, out var tid) ? tid : null;
                string? trainerName = trainerId is not null
                                      && trainerDisplays.TryGetValue(trainerId.Value, out var display)
                    ? display.FullName
                    : null;

                return new GymMembershipDto(
                    m.GymId,
                    m.Role.ToString(),
                    m.JoinedAt,
                    m.IsActive,
                    m.Section,
                    m.MemberNumber,
                    trainerId,
                    trainerName);
            })
            .ToList();

        var response = new UserProfileResponse(
            user.Id,
            user.Email.Value,
            user.PhoneNumber.Value,
            user.FirstName,
            user.LastName,
            user.ProfilePhotoUrl,
            user.DateOfBirth?.ToString("yyyy-MM-dd"),
            user.Gender.ToString(),
            user.Role.ToString(),
            user.Status.ToString(),
            user.Language.ToString(),
            gymMemberships);

        return Result.Success(response);
    }

    /// Identity is a global module — TenantResolutionMiddleware only sets a tenant
    /// context when the JWT carries gymId+schemaPrefix (post select-gym). Without
    /// that, ITrainerAssignmentLookup's BillingDbContext falls through to the
    /// legacy `billing` schema and returns no rows for any tenant. Fan out per
    /// gym instead so the trainee's "assigned trainer" resolves regardless of
    /// whether they've selected a gym.
    private async Task<IReadOnlyDictionary<Guid, Guid>> ResolveTrainersAcrossGymsAsync(
        Guid memberId,
        IReadOnlyList<Guid> gymIds,
        CancellationToken cancellationToken)
    {
        // If the request already has a resolved tenant context (e.g. trainee called
        // select-gym), the existing single-shot lookup is correct and cheaper.
        if (_tenantAccessor.IsResolved && gymIds.Count == 1 && gymIds[0] == _tenantAccessor.GymId)
            return await _trainerAssignmentLookup.GetAssignedTrainersAsync(memberId, gymIds, cancellationToken);

        var result = new Dictionary<Guid, Guid>();
        foreach (var gymId in gymIds)
        {
            try
            {
                var registration = await _gymRegistry.FindAsync(gymId, cancellationToken);
                if (registration is null) continue;

                using var scope = _scopeFactory.CreateScope();
                var sp = scope.ServiceProvider;
                var accessor = sp.GetRequiredService<ITenantContextAccessor>();
                accessor.Set(registration.GymId, registration.SchemaPrefix, isOwnerScope: false);

                var lookup = sp.GetRequiredService<ITrainerAssignmentLookup>();
                var perGym = await lookup.GetAssignedTrainersAsync(memberId, new[] { gymId }, cancellationToken);
                if (perGym.TryGetValue(gymId, out var trainerId))
                    result[gymId] = trainerId;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex,
                    "GetProfile: trainer-assignment lookup failed for member {MemberId} in gym {GymId}",
                    memberId, gymId);
            }
        }
        return result;
    }
}
