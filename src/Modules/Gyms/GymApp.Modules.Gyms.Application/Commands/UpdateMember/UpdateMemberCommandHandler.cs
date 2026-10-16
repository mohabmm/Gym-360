using GymApp.Modules.Identity.Shared;
using GymApp.Common.Application.Messaging;
using GymApp.Common.Domain;
using GymApp.Modules.Gyms.Application.Abstractions;
using GymApp.Modules.Gyms.Domain.Errors;
using GymApp.Modules.Gyms.Domain.Repositories;

namespace GymApp.Modules.Gyms.Application.Commands.UpdateMember;

internal sealed class UpdateMemberCommandHandler : ICommandHandler<UpdateMemberCommand>
{
    private readonly IGymMemberRepository _memberRepository;
    private readonly IUserProfileUpdater _profileUpdater;
    private readonly IGymsUnitOfWork _unitOfWork;

    public UpdateMemberCommandHandler(
        IGymMemberRepository memberRepository,
        IUserProfileUpdater profileUpdater,
        IGymsUnitOfWork unitOfWork)
    {
        _memberRepository = memberRepository;
        _profileUpdater = profileUpdater;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result> Handle(UpdateMemberCommand request, CancellationToken cancellationToken)
    {
        var member = await _memberRepository.GetAsync(request.GymId, request.MemberId, cancellationToken);
        if (member is null)
            return Result.Failure(GymErrors.MemberNotFound);

        if (request.Section is not null)
            member.SetSection(request.Section);

        string? firstName = null;
        string? lastName = null;
        if (!string.IsNullOrWhiteSpace(request.Name))
        {
            var parts = request.Name.Trim().Split(' ', 2);
            firstName = parts[0];
            lastName = parts.Length > 1 ? parts[1] : "";
        }

        var profileChanges = new UserProfileChanges(firstName, lastName, request.Phone);
        if (profileChanges.HasAny)
        {
            await _profileUpdater.UpdateAsync(request.MemberId, profileChanges, cancellationToken);
        }

        _memberRepository.Update(member);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}
