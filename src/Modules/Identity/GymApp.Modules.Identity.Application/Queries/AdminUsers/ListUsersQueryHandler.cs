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
        UserRole? roleFilter = null;
        if (!string.IsNullOrWhiteSpace(request.Role) && TryParseRole(request.Role, out var parsed))
            roleFilter = parsed;

        var users = await _userRepository.ListAsync(roleFilter, cancellationToken);
        var adminRoles = await _userRepository.GetAdminRolesForUsersAsync(
            users.Where(u => u.Role == UserRole.PlatformAdmin).Select(u => u.Id),
            cancellationToken);

        var gymsById = (await _gymRegistry.GetActiveAsync(cancellationToken))
            .ToDictionary(g => g.GymId, g => g.Name);

        var gymsByOwner = new Dictionary<Guid, IReadOnlyList<AdminUserGymDto>>();
        foreach (var ownerId in users.Where(u => u.OwnerId.HasValue).Select(u => u.OwnerId!.Value).Distinct())
        {
            var ownerGyms = await _gymRegistry.GetByOwnerAsync(ownerId, cancellationToken);
            gymsByOwner[ownerId] = ownerGyms
                .Select(g => new AdminUserGymDto(g.GymId, g.Name))
                .ToList();
        }

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
                Gyms: ResolveGyms(u.Role, u.OwnerId, u.GymMemberships, gymsById, gymsByOwner)))
            .ToList();

        return Result.Success(dtos);
    }

    private static IReadOnlyList<AdminUserGymDto> ResolveGyms(
        UserRole role,
        Guid? ownerId,
        IReadOnlyList<Domain.ValueObjects.GymMembership> memberships,
        IReadOnlyDictionary<Guid, string> gymsById,
        IReadOnlyDictionary<Guid, IReadOnlyList<AdminUserGymDto>> gymsByOwner)
    {
        if (role == UserRole.GymOwner && ownerId.HasValue && gymsByOwner.TryGetValue(ownerId.Value, out var ownerGyms))
            return ownerGyms;

        if (role == UserRole.PlatformAdmin)
            return Array.Empty<AdminUserGymDto>();

        return memberships
            .Where(m => m.IsActive)
            .Select(m => new AdminUserGymDto(
                m.GymId,
                gymsById.TryGetValue(m.GymId, out var name) ? name : string.Empty))
            .ToList();
    }

    private static bool TryParseRole(string raw, out UserRole role)
    {
        role = raw.ToLowerInvariant() switch
        {
            "trainee" => UserRole.Trainee,
            "trainer" => UserRole.Trainer,
            "gym_staff" or "staff" => UserRole.GymStaff,
            "gym_owner" or "owner" => UserRole.GymOwner,
            "platform_admin" or "admin" => UserRole.PlatformAdmin,
            _ => UserRole.Trainee
        };
        return Enum.IsDefined(typeof(UserRole), role) && raw.Length > 0;
    }

    private static string MapRoleClaim(UserRole role) => role switch
    {
        UserRole.Trainee => "trainee",
        UserRole.Trainer => "trainer",
        UserRole.GymStaff => "gym_staff",
        UserRole.GymOwner => "gym_owner",
        UserRole.PlatformAdmin => "platform_admin",
        _ => role.ToString().ToLowerInvariant()
    };

    private static string MapAdminRole(AdminRole role) => role switch
    {
        AdminRole.SuperAdmin => "super_admin",
        AdminRole.Admin => "admin",
        AdminRole.Support => "support",
        AdminRole.Analyst => "analyst",
        _ => role.ToString().ToLowerInvariant()
    };

    private static string MapStatus(UserStatus status) => status switch
    {
        UserStatus.PendingVerification => "pending",
        UserStatus.Active => "active",
        UserStatus.Suspended => "suspended",
        UserStatus.Deleted => "inactive",
        _ => status.ToString().ToLowerInvariant()
    };
}
