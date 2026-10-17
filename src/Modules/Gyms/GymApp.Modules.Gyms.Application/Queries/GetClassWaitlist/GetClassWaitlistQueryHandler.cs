using GymApp.Common.Application.Messaging;
using GymApp.Common.Domain;
using GymApp.Modules.Gyms.Application.Contracts;
using GymApp.Modules.Gyms.Application.DTOs;
using GymApp.Modules.Gyms.Domain.Errors;
using GymApp.Modules.Gyms.Domain.ValueObjects;

namespace GymApp.Modules.Gyms.Application.Queries.GetClassWaitlist;

internal sealed class GetClassWaitlistQueryHandler : IQueryHandler<GetClassWaitlistQuery, List<MemberDto>>
{
    private readonly IGymQueryService _gymQueryService;

    public GetClassWaitlistQueryHandler(IGymQueryService gymQueryService)
    {
        _gymQueryService = gymQueryService;
    }

    public async Task<Result<List<MemberDto>>> Handle(GetClassWaitlistQuery request, CancellationToken cancellationToken)
    {
        var members = await _gymQueryService.GetClassWaitlistAsync(new GymId(request.GymId), request.ClassId, cancellationToken);

        if (members is null)
            return Result.Failure<List<MemberDto>>(GymErrors.ClassNotFound);

        return Result.Success(members);
    }
}
