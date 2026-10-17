using GymApp.Common.Application.Messaging;
using GymApp.Common.Application.Tenancy;
using GymApp.Common.Domain;
using GymApp.Modules.Identity.Application.DTOs;
using GymApp.Modules.Identity.Domain.Enums;
using GymApp.Modules.Identity.Domain.Repositories;

namespace GymApp.Modules.Identity.Application.Queries.AdminUsers;

internal sealed class ListUsersQueryHandler : IQueryHandler<ListUsersQuery, List<AdminUserDto>>
{
    private readonly IUserRepository _userRepository;
    private readonly IGymRegistry _gymRegistry;

    public ListUsersQueryHandler(IUserRepository userRepository, IGymRegistry gymRegistry)
    {
        _userRepository = userRepository;
        _gymRegistry = gymRegistry;
    }

    public async Task<Result<List<AdminUserDto>>> Handle(ListUsersQuery request, CancellationToken cancellationToken)
    {
        var roleFilter = ParseRoleFilter(request.Role);

        var users = await _userRepository.ListAsync(roleFilter, cancellationToken);
        var adminRoles = await _userRepository.GetAdminRolesForUsersAsync(
            users.Where(u => u.Role == UserRole.PlatformAdmin).Select(u => u.Id),
            cancellationToken);

        var gymsById = (await _gymRegistry.GetActiveAsync(cancellationToken))
            .ToDictionary(g => g.GymId, g => g.Name);

        var gyms = new GymLookups(gymsById, await LoadGymsByOwnerAsync(users, cancellationToken));

        var dtos = users
            .Select(u => new AdminUserDto(
                u.Id,
                u.Email.Value,
                u.PhoneNumber.Value,
                u.FirstName,
                u.LastName,
                FirstNameAr: string.Empty,
                LastNameAr: string.Empty,
                Role: MapRoleClaim(u.Role),
                AdminRole: adminRoles.TryGetValue(u.Id, out var ar) ? MapAdminRole(ar) : null,
                Status: MapStatus(u.Status),
                CreatedAt: u.CreatedAt.ToString("O"),
                LastLoginAt: null,
                IsYou: false,
                Gyms: ResolveGyms(u, gyms)))
            .ToList();

        return Result.Success(dtos);
    }

    private sealed record GymLookups(
        IReadOnlyDictionary<Guid, string> GymsById,
        IReadOnlyDictionary<Guid, IReadOnlyList<AdminUserGymDto>> GymsByOwner);

    private async Task<Dictionary<Guid, IReadOnlyList<AdminUserGymDto>>> LoadGymsByOwnerAsync(
        IEnumerable<Domain.Entities.User> users,
        CancellationToken cancellationToken)
    {
        var gymsByOwner = new Dictionary<Guid, IReadOnlyList<AdminUserGymDto>>();
        foreach (var ownerId in users.Where(u => u.OwnerId.HasValue).Select(u => u.OwnerId!.Value).Distinct())
        {
            var ownerGyms = await _gymRegistry.GetByOwnerAsync(ownerId, cancellationToken);
            gymsByOwner[ownerId] = ownerGyms
                .Select(g => new AdminUserGymDto(g.GymId, g.Name))
                .ToList();
        }

        return gymsByOwner;
    }

    private static IReadOnlyList<AdminUserGymDto> ResolveGyms(Domain.Entities.User user, GymLookups gyms)
    {
        if (TryGetOwnedGyms(user, gyms.GymsByOwner, out var ownerGyms))
            return ownerGyms;

        if (user.Role == UserRole.PlatformAdmin)
            return Array.Empty<AdminUserGymDto>();

        return user.GymMemberships
            .Where(m => m.IsActive)
            .Select(m => new AdminUserGymDto(
                m.GymId,
                gyms.GymsById.TryGetValue(m.GymId, out var name) ? name : string.Empty))
            .ToList();
    }

    private static bool TryGetOwnedGyms(
        Domain.Entities.User user,
        IReadOnlyDictionary<Guid, IReadOnlyList<AdminUserGymDto>> gymsByOwner,
        out IReadOnlyList<AdminUserGymDto> ownerGyms)
    {
        ownerGyms = Array.Empty<AdminUserGymDto>();
        var isOwnerWithAggregate = user.Role == UserRole.GymOwner && user.OwnerId.HasValue;
        return isOwnerWithAggregate && gymsByOwner.TryGetValue(user.OwnerId!.Value, out ownerGyms!);
    }

    private static readonly Dictionary<string, UserRole> RoleAliases = new()
    {
        ["trainee"] = UserRole.Trainee,
        ["trainer"] = UserRole.Trainer,
        ["gym_staff"] = UserRole.GymStaff,
        ["staff"] = UserRole.GymStaff,
        ["gym_owner"] = UserRole.GymOwner,
        ["owner"] = UserRole.GymOwner,
        ["platform_admin"] = UserRole.PlatformAdmin,
        ["admin"] = UserRole.PlatformAdmin
    };

    // Blank → no filter. Unrecognized values fall back to Trainee (existing behavior).
    private static UserRole? ParseRoleFilter(string? raw) =>
        string.IsNullOrWhiteSpace(raw)
            ? null
            : RoleAliases.GetValueOrDefault(raw.ToLowerInvariant(), UserRole.Trainee);

    private static readonly Dictionary<UserRole, string> RoleClaims = new()
    {
        [UserRole.Trainee] = "trainee",
        [UserRole.Trainer] = "trainer",
        [UserRole.GymStaff] = "gym_staff",
        [UserRole.GymOwner] = "gym_owner",
        [UserRole.PlatformAdmin] = "platform_admin"
    };

    private static readonly Dictionary<AdminRole, string> AdminRoleClaims = new()
    {
        [AdminRole.SuperAdmin] = "super_admin",
        [AdminRole.Admin] = "admin",
        [AdminRole.Support] = "support",
        [AdminRole.Analyst] = "analyst"
    };

    private static readonly Dictionary<UserStatus, string> StatusLabels = new()
    {
        [UserStatus.PendingVerification] = "pending",
        [UserStatus.Active] = "active",
        [UserStatus.Suspended] = "suspended",
        [UserStatus.Deleted] = "inactive"
    };

    private static string MapRoleClaim(UserRole role) => LookupOrLowerName(RoleClaims, role);

    private static string MapAdminRole(AdminRole role) => LookupOrLowerName(AdminRoleClaims, role);

    private static string MapStatus(UserStatus status) => LookupOrLowerName(StatusLabels, status);

    // Unmapped enum values fall back to their lower-cased name.
    private static string LookupOrLowerName<TEnum>(IReadOnlyDictionary<TEnum, string> map, TEnum value)
        where TEnum : struct, Enum =>
        map.TryGetValue(value, out var label) ? label : value.ToString().ToLowerInvariant();
}
