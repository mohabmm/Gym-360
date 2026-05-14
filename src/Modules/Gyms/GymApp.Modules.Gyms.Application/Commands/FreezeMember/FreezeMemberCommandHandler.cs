using GymApp.Common.Application.Messaging;
using GymApp.Common.Domain;
using GymApp.Modules.Gyms.Application.Abstractions;
using GymApp.Modules.Gyms.Domain.Errors;
using GymApp.Modules.Gyms.Domain.Repositories;

namespace GymApp.Modules.Gyms.Application.Commands.FreezeMember;

internal sealed class FreezeMemberCommandHandler : ICommandHandler<FreezeMemberCommand>
{
    private readonly IGymMemberRepository _memberRepository;
    private readonly IGymsUnitOfWork _unitOfWork;

    public FreezeMemberCommandHandler(IGymMemberRepository memberRepository, IGymsUnitOfWork unitOfWork)
    {
        _memberRepository = memberRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result> Handle(FreezeMemberCommand request, CancellationToken cancellationToken)
    {
        var member = await _memberRepository.GetAsync(request.GymId, request.MemberId, cancellationToken);
        if (member is null)
            return Result.Failure(GymErrors.MemberNotFound);

        var result = member.Freeze();
        if (result.IsFailure)
            return result;

        _memberRepository.Update(member);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
