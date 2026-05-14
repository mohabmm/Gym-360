using GymApp.Common.Application.Messaging;
using GymApp.Common.Domain;
using GymApp.Modules.Identity.Application.Abstractions;
using GymApp.Modules.Identity.Domain.Errors;
using GymApp.Modules.Identity.Domain.Repositories;

namespace GymApp.Modules.Identity.Application.Commands.AdminUsers;

internal sealed class DeleteAdminUserCommandHandler : ICommandHandler<DeleteAdminUserCommand>
{
    private readonly IUserRepository _userRepository;
    private readonly IIdentityUnitOfWork _unitOfWork;

    public DeleteAdminUserCommandHandler(
        IUserRepository userRepository,
        IIdentityUnitOfWork unitOfWork)
    {
        _userRepository = userRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result> Handle(DeleteAdminUserCommand request, CancellationToken cancellationToken)
    {
        // Guard: an admin cannot remove their own admin access — they'd lock themselves out.
        if (request.RequestedBy.HasValue && request.RequestedBy.Value == request.UserId)
            return Result.Failure(new Error("AdminUser.CannotDeleteSelf", "You cannot remove your own admin access."));

        var user = await _userRepository.GetByIdAsync(request.UserId, cancellationToken);
        if (user is null)
            return Result.Failure(UserErrors.NotFound(request.UserId));

        var platformAdmin = await _userRepository.GetPlatformAdminByUserIdAsync(request.UserId, cancellationToken);
        if (platformAdmin is not null)
            _userRepository.RemovePlatformAdmin(platformAdmin);

        // Soft-delete the user row so historical audit/outbox data stays referentially sound.
        user.MarkDeleted();
        _userRepository.Update(user);

        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}
