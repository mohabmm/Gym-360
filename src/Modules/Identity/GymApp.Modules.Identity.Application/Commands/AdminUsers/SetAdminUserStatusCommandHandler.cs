using GymApp.Common.Application.Messaging;
using GymApp.Common.Domain;
using GymApp.Modules.Identity.Application.Abstractions;
using GymApp.Modules.Identity.Domain.Entities;
using GymApp.Modules.Identity.Domain.Enums;
using GymApp.Modules.Identity.Domain.Errors;
using GymApp.Modules.Identity.Domain.Repositories;

namespace GymApp.Modules.Identity.Application.Commands.AdminUsers;

/// <summary>
/// Suspend: status → Suspended, every refresh token revoked, lockout cache set so the
/// user's current access token is rejected on their next request.
/// Reactivate: status → Active and lockout cache cleared.
/// </summary>
internal sealed class SetAdminUserStatusCommandHandler : ICommandHandler<SetAdminUserStatusCommand>
{
    internal const string Active = "active";
    internal const string Suspended = "suspended";

    private readonly IUserRepository _userRepository;
    private readonly IIdentityUnitOfWork _unitOfWork;
    private readonly IUserLockoutCache _lockoutCache;

    public SetAdminUserStatusCommandHandler(
        IUserRepository userRepository,
        IIdentityUnitOfWork unitOfWork,
        IUserLockoutCache lockoutCache)
    {
        _userRepository = userRepository;
        _unitOfWork = unitOfWork;
        _lockoutCache = lockoutCache;
    }

    public async Task<Result> Handle(SetAdminUserStatusCommand request, CancellationToken cancellationToken)
    {
        if (request.RequestedBy == request.UserId)
            return Result.Failure(AdminUserErrors.CannotChangeOwnStatus);

        var user = await _userRepository.GetByIdWithRefreshTokensAsync(request.UserId, cancellationToken);
        if (user is null)
            return Result.Failure(UserErrors.NotFound(request.UserId));

        return request.Status switch
        {
            Suspended => await SuspendAsync(user, cancellationToken),
            Active => await ReactivateAsync(user, cancellationToken),
            _ => Result.Failure(AdminUserErrors.InvalidStatus)
        };
    }

    private async Task<Result> SuspendAsync(User user, CancellationToken cancellationToken)
    {
        user.Suspend();
        user.RevokeAllRefreshTokens();
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        await _lockoutCache.MarkBlockedAsync(user.Id, cancellationToken: cancellationToken);
        return Result.Success();
    }

    private async Task<Result> ReactivateAsync(User user, CancellationToken cancellationToken)
    {
        if (user.Status == UserStatus.Deleted)
            return Result.Failure(AdminUserErrors.CannotReactivateDeleted);

        user.Activate();
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        await _lockoutCache.ClearAsync(user.Id, cancellationToken);
        return Result.Success();
    }
}
