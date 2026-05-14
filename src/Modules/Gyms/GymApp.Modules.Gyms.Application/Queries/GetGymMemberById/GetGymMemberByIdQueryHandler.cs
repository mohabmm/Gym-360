using GymApp.Common.Application.Messaging;
using GymApp.Common.Domain;
using GymApp.Modules.Gyms.Application.Contracts;
using GymApp.Modules.Gyms.Application.DTOs;
using GymApp.Modules.Gyms.Domain.Errors;

namespace GymApp.Modules.Gyms.Application.Queries.GetGymMemberById;

internal sealed class GetGymMemberByIdQueryHandler : IQueryHandler<GetGymMemberByIdQuery, MemberDto>
{
    private readonly IGymQueryService _gymQueryService;

    public GetGymMemberByIdQueryHandler(IGymQueryService gymQueryService)
    {
        _gymQueryService = gymQueryService;
    }

    public async Task<Result<MemberDto>> Handle(GetGymMemberByIdQuery request, CancellationToken cancellationToken)
    {
        var member = await _gymQueryService.GetMemberAsync(request.GymId, request.MemberId, cancellationToken);

        if (member is null)
            return Result.Failure<MemberDto>(GymErrors.MemberNotFound);

        return Result.Success(member);
    }
}
