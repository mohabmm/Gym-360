using GymApp.Modules.Identity.Shared;
using GymApp.Common.Application.Messaging;
using GymApp.Common.Domain;
using GymApp.Modules.Gyms.Application.DTOs;
using GymApp.Modules.Gyms.Domain.Entities;
using GymApp.Modules.Gyms.Domain.Enums;
using GymApp.Modules.Gyms.Domain.Errors;
using GymApp.Modules.Gyms.Domain.Repositories;
using GymApp.Modules.Gyms.Application.Abstractions;

namespace GymApp.Modules.Gyms.Application.Commands.RecordCheckIn;

internal sealed class RecordCheckInCommandHandler : ICommandHandler<RecordCheckInCommand, CheckInResultDto>
{
    private readonly IGymRepository _gymRepository;
    private readonly ICheckInRepository _checkInRepository;
    private readonly CheckInEligibilityChecker _eligibilityChecker;
    private readonly IUserProfileLookup _userProfileLookup;
    private readonly IGymsUnitOfWork _unitOfWork;

    public RecordCheckInCommandHandler(
        IGymRepository gymRepository,
        ICheckInRepository checkInRepository,
        CheckInEligibilityChecker eligibilityChecker,
        IUserProfileLookup userProfileLookup,
        IGymsUnitOfWork unitOfWork)
    {
        _gymRepository = gymRepository;
        _checkInRepository = checkInRepository;
        _eligibilityChecker = eligibilityChecker;
        _userProfileLookup = userProfileLookup;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<CheckInResultDto>> Handle(RecordCheckInCommand request, CancellationToken cancellationToken)
    {
        var gym = await _gymRepository.GetByIdAsync(request.GymId, cancellationToken);
        if (gym is null)
            return Result.Failure<CheckInResultDto>(GymErrors.NotFound);

        if (!Enum.TryParse<CheckInMethod>(request.Method, ignoreCase: true, out var method))
            return Result.Failure<CheckInResultDto>(GymErrors.InvalidCheckIn);

        var eligibleMember = await _eligibilityChecker.ResolveEligibleMemberAsync(request, cancellationToken);
        if (eligibleMember.IsFailure)
            return Result.Failure<CheckInResultDto>(eligibleMember.Error);

        var memberId = eligibleMember.Value;
        var checkIn = CheckIn.Create(request.GymId, memberId, method, request.StaffId);

        await _checkInRepository.AddAsync(checkIn, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        var profile = await _userProfileLookup.GetProfileAsync(memberId, cancellationToken);
        var fullName = profile is null ? null : $"{profile.FirstName} {profile.LastName}".Trim();

        var dto = new CheckInResultDto(
            Success: true,
            CheckInId: checkIn.Id,
            MemberName: fullName,
            MemberPhotoUrl: profile?.ProfilePhotoUrl,
            GymName: gym.Name);

        return Result.Success(dto);
    }
}
