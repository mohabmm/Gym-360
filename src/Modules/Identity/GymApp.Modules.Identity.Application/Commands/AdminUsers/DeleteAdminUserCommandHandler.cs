using GymApp.Common.Application.Messaging;
using GymApp.Common.Application.Tenancy;
using GymApp.Common.Domain;
using GymApp.Modules.Identity.Application.Abstractions;
using GymApp.Modules.Identity.Domain.Entities;
using GymApp.Modules.Identity.Domain.Enums;
using GymApp.Modules.Identity.Domain.Errors;
using GymApp.Modules.Identity.Domain.Repositories;

namespace GymApp.Modules.Identity.Application.Commands.AdminUsers;

/// <summary>
/// Permanent (hard) delete by a platform admin. Order is chosen so a failure part-way
/// is safe to retry: (1) guards, (2) lock the user out, (3) every module erases the
/// user's data across all gyms, (4) Identity rows removed last.
/// </summary>
internal sealed class DeleteAdminUserCommandHandler : ICommandHandler<DeleteAdminUserCommand>
{
    private readonly IUserRepository _userRepository;
    private readonly IIdentityUnitOfWork _unitOfWork;
    private readonly IGymRegistry _gymRegistry;
    private readonly IUserLockoutCache _lockoutCache;
    private readonly IUserDataPurger _dataPurger;

    public DeleteAdminUserCommandHandler(
        IUserRepository userRepository,
        IIdentityUnitOfWork unitOfWork,
        IGymRegistry gymRegistry,
        IUserLockoutCache lockoutCache,
        IUserDataPurger dataPurger)
    {
        _userRepository = userRepository;
        _unitOfWork = unitOfWork;
        _gymRegistry = gymRegistry;
        _lockoutCache = lockoutCache;
        _dataPurger = dataPurger;
    }

    public async Task<Result> Handle(DeleteAdminUserCommand request, CancellationToken cancellationToken)
    {
        if (request.RequestedBy == request.UserId)
            return Result.Failure(AdminUserErrors.CannotDeleteSelf);

        var user = await _userRepository.GetByIdWithRefreshTokensAsync(request.UserId, cancellationToken);
        if (user is null)
            return Result.Failure(UserErrors.NotFound(request.UserId));

        var guard = await CheckDeletableAsync(user, cancellationToken);
        if (guard.IsFailure)
            return guard;

        await LockOutAsync(user, cancellationToken);

        await _dataPurger.PurgeAsync(user.Id, cancellationToken);

        await _userRepository.DeleteSatelliteRecordsAsync(user.Id, cancellationToken);
        _userRepository.Remove(user);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }

    private async Task<Result> CheckDeletableAsync(User user, CancellationToken cancellationToken)
    {
        if (user.OwnerId.HasValue)
        {
            var gyms = await _gymRegistry.GetByOwnerAsync(user.OwnerId.Value, cancellationToken);
            if (gyms.Count > 0)
                return Result.Failure(AdminUserErrors.OwnerHasGyms);
        }

        var admin = await _userRepository.GetPlatformAdminByUserIdAsync(user.Id, cancellationToken);
        if (admin?.AdminRole == AdminRole.SuperAdmin
            && await _userRepository.CountSuperAdminsAsync(cancellationToken) <= 1)
            return Result.Failure(AdminUserErrors.CannotDeleteLastSuperAdmin);

        return Result.Success();
    }

    private async Task LockOutAsync(User user, CancellationToken cancellationToken)
    {
        user.Suspend();
        user.RevokeAllRefreshTokens();
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        await _lockoutCache.MarkDeletedAsync(user.Id, cancellationToken);
    }
}
