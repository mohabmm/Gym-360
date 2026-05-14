using GymApp.Common.Application.Messaging;
using GymApp.Common.Application.Tenancy;
using GymApp.Common.Domain;
using GymApp.Modules.Identity.Application.Abstractions;
using GymApp.Modules.Identity.Domain.Enums;
using GymApp.Modules.Identity.Domain.Errors;
using GymApp.Modules.Identity.Domain.Repositories;

namespace GymApp.Modules.Identity.Application.Commands.LinkToGym;

internal sealed class LinkToGymCommandHandler : ICommandHandler<LinkToGymCommand>
{
    private readonly IUserRepository _userRepository;
    private readonly IIdentityUnitOfWork _unitOfWork;
    private readonly IGymRegistry _gymRegistry;

    public LinkToGymCommandHandler(
        IUserRepository userRepository,
        IIdentityUnitOfWork unitOfWork,
        IGymRegistry gymRegistry)
    {
        _userRepository = userRepository;
        _unitOfWork = unitOfWork;
        _gymRegistry = gymRegistry;
    }

    public async Task<Result> Handle(LinkToGymCommand request, CancellationToken cancellationToken)
    {
        var user = await _userRepository.GetByIdAsync(request.UserId, cancellationToken);
        if (user is null)
            return Result.Failure(UserErrors.NotFound(request.UserId));

        var registration = await _gymRegistry.FindByCodeAsync(request.GymCode, cancellationToken);
        if (registration is null)
            return Result.Failure(new Error("Gyms.GymCodeNotFound", $"No active gym matches code '{request.GymCode}'."));

        // Reuse existing user-side LinkToGym domain method. Note: Trainees require a
        // memberNumber for QR check-in — this self-serve path does not allocate one,
        // so trainees should still go through staff-create. Owners/Trainers/Staff
        // (no memberNumber required) can self-serve link via gym code.
        if (user.Role == UserRole.Trainee)
            return Result.Failure(new Error("User.TraineeMustUseStaffCreate",
                "Trainees cannot self-link by gym code; staff must register them via /identity/users/staff-create so a MemberNumber is allocated."));

        var link = user.LinkToGym(registration.GymId, user.Role, section: null, memberNumber: null);
        if (link.IsFailure)
            return Result.Failure(link.Error);

        _userRepository.Update(user);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}
