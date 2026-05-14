using GymApp.Common.Application.Messaging;
using GymApp.Common.Domain;
using GymApp.Modules.Identity.Application.Abstractions;
using GymApp.Modules.Identity.Domain.Errors;
using GymApp.Modules.Identity.Domain.Repositories;

namespace GymApp.Modules.Identity.Application.Commands.LinkMemberToGym;

internal sealed class LinkMemberToGymCommandHandler : ICommandHandler<LinkMemberToGymCommand>
{
    private readonly IUserRepository _userRepository;
    private readonly IIdentityUnitOfWork _unitOfWork;

    public LinkMemberToGymCommandHandler(
        IUserRepository userRepository,
        IIdentityUnitOfWork unitOfWork)
    {
        _userRepository = userRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result> Handle(LinkMemberToGymCommand request, CancellationToken cancellationToken)
    {
        var user = await _userRepository.GetByIdAsync(request.UserId, cancellationToken);
        if (user is null)
            return Result.Failure(UserErrors.NotFound(request.UserId));

        if (user.GymMemberships.Any(m => m.GymId == request.GymId && m.Role == request.Role && m.IsActive))
            return Result.Success();

        var link = user.LinkToGym(request.GymId, request.Role, request.Section, request.MemberNumber);
        if (link.IsFailure)
            return Result.Failure(link.Error);

        _userRepository.Update(user);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}
