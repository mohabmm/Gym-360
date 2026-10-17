using GymApp.Common.Domain;
using GymApp.Modules.Gyms.Application.Contracts;
using GymApp.Modules.Gyms.Domain.Errors;
using GymApp.Modules.Gyms.Domain.Repositories;

namespace GymApp.Modules.Gyms.Application.Commands.RecordCheckIn;

/// <summary>
/// Resolves which member a check-in request is for (direct id, QR token or member number)
/// and verifies the member may check in today (not frozen, not already checked in).
/// </summary>
internal sealed class CheckInEligibilityChecker
{
    private readonly IGymMemberDirectory _memberDirectory;
    private readonly IGymMemberRepository _gymMemberRepository;
    private readonly ICheckInRepository _checkInRepository;

    public CheckInEligibilityChecker(
        IGymMemberDirectory memberDirectory,
        IGymMemberRepository gymMemberRepository,
        ICheckInRepository checkInRepository)
    {
        _memberDirectory = memberDirectory;
        _gymMemberRepository = gymMemberRepository;
        _checkInRepository = checkInRepository;
    }

    public async Task<Result<Guid>> ResolveEligibleMemberAsync(RecordCheckInCommand request, CancellationToken cancellationToken)
    {
        // Resolve member ID based on check-in method
        var memberId = await ResolveMemberIdAsync(request, cancellationToken);
        if (memberId is null)
            return Result.Failure<Guid>(GymErrors.InvalidCheckIn);

        var gymMember = await _gymMemberRepository.GetAsync(request.GymId, memberId.Value, cancellationToken);
        if (gymMember?.IsFrozen == true)
            return Result.Failure<Guid>(GymErrors.MemberFrozen);

        var latestCheckIn = await _checkInRepository.GetLatestForMemberAsync(
            request.GymId, memberId.Value, cancellationToken);

        if (latestCheckIn is not null && latestCheckIn.CheckedInAt.Date == DateTime.UtcNow.Date)
            return Result.Failure<Guid>(GymErrors.DuplicateCheckIn);

        return Result.Success(memberId.Value);
    }

    private async Task<Guid?> ResolveMemberIdAsync(RecordCheckInCommand request, CancellationToken cancellationToken)
    {
        if (request.MemberId.HasValue)
            return request.MemberId.Value;

        var memberNumber = MemberNumberFrom(request);
        if (memberNumber is null)
            return null;

        return await _memberDirectory.FindMemberIdByNumberAsync(request.GymId, memberNumber, cancellationToken);
    }

    // A QR token carries the member number; it wins over a typed member number.
    private static string? MemberNumberFrom(RecordCheckInCommand request)
    {
        if (!string.IsNullOrWhiteSpace(request.QrToken))
            return request.QrToken.Trim();

        if (!string.IsNullOrWhiteSpace(request.MemberNumber))
            return request.MemberNumber;

        return null;
    }
}
