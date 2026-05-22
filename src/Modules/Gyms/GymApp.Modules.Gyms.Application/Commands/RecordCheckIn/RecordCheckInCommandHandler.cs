using GymApp.Modules.Identity.Shared;
using GymApp.Common.Application.Messaging;
using GymApp.Common.Domain;
using GymApp.Modules.Gyms.Application.Contracts;
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
    private readonly IGymMemberDirectory _memberDirectory;
    private readonly IGymMemberRepository _gymMemberRepository;
    private readonly IUserProfileLookup _userProfileLookup;
    private readonly IGymsUnitOfWork _unitOfWork;

    public RecordCheckInCommandHandler(
        IGymRepository gymRepository,
        ICheckInRepository checkInRepository,
        IGymMemberDirectory memberDirectory,
        IGymMemberRepository gymMemberRepository,
        IUserProfileLookup userProfileLookup,
        IGymsUnitOfWork unitOfWork)
    {
        _gymRepository = gymRepository;
        _checkInRepository = checkInRepository;
        _memberDirectory = memberDirectory;
        _gymMemberRepository = gymMemberRepository;
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

        // Resolve member ID based on check-in method
        Guid memberId;

        if (request.MemberId.HasValue)
        {
            memberId = request.MemberId.Value;
        }
        else if (!string.IsNullOrWhiteSpace(request.QrToken))
        {
            var resolved = await _memberDirectory.FindMemberIdByNumberAsync(
                request.GymId, request.QrToken.Trim(), cancellationToken);
            if (resolved is null)
                return Result.Failure<CheckInResultDto>(GymErrors.InvalidCheckIn);
            memberId = resolved.Value;
        }
        else if (!string.IsNullOrWhiteSpace(request.MemberNumber))
        {
            var resolved = await _memberDirectory.FindMemberIdByNumberAsync(
                request.GymId, request.MemberNumber, cancellationToken);
            if (resolved is null)
                return Result.Failure<CheckInResultDto>(GymErrors.InvalidCheckIn);
            memberId = resolved.Value;
        }
        else
        {
            return Result.Failure<CheckInResultDto>(GymErrors.InvalidCheckIn);
        }

        var gymMember = await _gymMemberRepository.GetAsync(request.GymId, memberId, cancellationToken);
        if (gymMember?.IsFrozen == true)
            return Result.Failure<CheckInResultDto>(GymErrors.MemberFrozen);

        var latestCheckIn = await _checkInRepository.GetLatestForMemberAsync(
            request.GymId, memberId, cancellationToken);

        if (latestCheckIn is not null && latestCheckIn.CheckedInAt.Date == DateTime.UtcNow.Date)
            return Result.Failure<CheckInResultDto>(GymErrors.DuplicateCheckIn);

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
