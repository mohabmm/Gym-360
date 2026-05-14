using GymApp.Common.Application.Messaging;
using GymApp.Common.Application.Tenancy;
using GymApp.Common.Domain;
using GymApp.Modules.Identity.Application.DTOs;
using GymApp.Modules.Identity.Domain.Enums;
using GymApp.Modules.Identity.Domain.Errors;
using GymApp.Modules.Identity.Domain.Repositories;

namespace GymApp.Modules.Identity.Application.Queries.AdminUsers;

internal sealed class GetAdminUserByIdQueryHandler : IQueryHandler<GetAdminUserByIdQuery, AdminUserDto>
{
    private readonly IUserRepository _userRepository;
    private readonly IGymRegistry _gymRegistry;

    public GetAdminUserByIdQueryHandler(IUserRepository userRepository, IGymRegistry gymRegistry)
    {
        _userRepository = userRepository;
        _gymRegistry = gymRegistry;
    }

    public async Task<Result<AdminUserDto>> Handle(GetAdminUserByIdQuery request, CancellationToken cancellationToken)
    {
        var user = await _userRepository.GetByIdAsync(request.UserId, cancellationToken);
        if (user is null)
            return Result.Failure<AdminUserDto>(UserErrors.NotFound(request.UserId));

        string? adminRole = null;
        if (user.Role == UserRole.PlatformAdmin)
        {
            var roles = await _userRepository.GetAdminRolesForUsersAsync(new[] { user.Id }, cancellationToken);
            if (roles.TryGetValue(user.Id, out var ar))
                adminRole = MapAdminRole(ar);
        }

        var gymsById = (await _gymRegistry.GetActiveAsync(cancellationToken))
            .ToDictionary(g => g.GymId, g => g.Name);

        IReadOnlyList<AdminUserGymDto> gyms;
        if (user.Role == UserRole.GymOwner && user.OwnerId.HasValue)
        {
            var ownerGyms = await _gymRegistry.GetByOwnerAsync(user.OwnerId.Value, cancellationToken);
            gyms = ownerGyms.Select(g => new AdminUserGymDto(g.GymId, g.Name)).ToList();
        }
        else if (user.Role == UserRole.PlatformAdmin)
        {
            gyms = Array.Empty<AdminUserGymDto>();
        }
        else
        {
            gyms = user.GymMemberships
                .Where(m => m.IsActive)
                .Select(m => new AdminUserGymDto(m.GymId, gymsById.TryGetValue(m.GymId, out var name) ? name : string.Empty))
                .ToList();
        }

        var dto = new AdminUserDto(
            user.Id,
            user.Email.Value,
            user.PhoneNumber.Value,
            user.FirstName,
            user.LastName,
            FirstNameAr: string.Empty,
            LastNameAr: string.Empty,
            Role: MapRoleClaim(user.Role),
            AdminRole: adminRole,
            Status: MapStatus(user.Status),
            CreatedAt: user.CreatedAt.ToString("O"),
            LastLoginAt: null,
            IsYou: false,
            Gyms: gyms);

        return Result.Success(dto);
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
