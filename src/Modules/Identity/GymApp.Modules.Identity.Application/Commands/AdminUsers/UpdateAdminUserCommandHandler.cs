using GymApp.Common.Application.Abstractions;
using GymApp.Common.Application.Messaging;
using GymApp.Common.Domain;
using GymApp.Modules.Identity.Domain.Errors;

namespace GymApp.Modules.Identity.Application.Commands.AdminUsers;

internal sealed class UpdateAdminUserCommandHandler : ICommandHandler<UpdateAdminUserCommand>
{
    private readonly IUserProfileUpdater _profileUpdater;

    public UpdateAdminUserCommandHandler(IUserProfileUpdater profileUpdater)
    {
        _profileUpdater = profileUpdater;
    }

    public async Task<Result> Handle(UpdateAdminUserCommand request, CancellationToken cancellationToken)
    {
        var ok = await _profileUpdater.UpdateAsync(
            request.UserId, request.FirstName, request.LastName, request.Phone, cancellationToken);
        return ok ? Result.Success() : Result.Failure(UserErrors.NotFound(request.UserId));
    }
}
