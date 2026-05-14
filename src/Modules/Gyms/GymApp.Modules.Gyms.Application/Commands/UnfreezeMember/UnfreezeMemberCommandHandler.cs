using GymApp.Common.Application.Messaging;
using GymApp.Common.Domain;
using GymApp.Modules.Gyms.Application.Abstractions;
using GymApp.Modules.Gyms.Domain.Errors;
using GymApp.Modules.Gyms.Domain.Repositories;

namespace GymApp.Modules.Gyms.Application.Commands.UnfreezeMember;

internal sealed class UnfreezeMemberCommandHandler : ICommandHandler<UnfreezeMemberCommand>
{
    private readonly IGymMemberRepository _memberRepository;
    private readonly IGymsUnitOfWork _unitOfWork;

    public UnfreezeMemberCommandHandler(IGymMemberRepository memberRepository, IGymsUnitOfWork unitOfWork)
    {
        _memberRepository = memberRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result> Handle(UnfreezeMemberCommand request, CancellationToken cancellationToken)
    {
        var member = await _memberRepository.GetAsync(request.GymId, request.MemberId, cancellationToken);
        if (member is null)
            return Result.Failure(GymErrors.MemberNotFound);

        var result = member.Unfreeze();
        if (result.IsFailure)
            return result;

        _memberRepository.Update(member);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
