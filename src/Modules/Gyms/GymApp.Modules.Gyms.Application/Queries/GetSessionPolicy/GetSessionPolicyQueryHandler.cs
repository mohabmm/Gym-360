using GymApp.Common.Application.Messaging;
using GymApp.Common.Domain;
using GymApp.Modules.Gyms.Application.Contracts;
using GymApp.Modules.Gyms.Application.DTOs;
using GymApp.Modules.Gyms.Domain.ValueObjects;

namespace GymApp.Modules.Gyms.Application.Queries.GetSessionPolicy;

internal sealed class GetSessionPolicyQueryHandler : IQueryHandler<GetSessionPolicyQuery, SessionPolicyDto>
{
    private readonly IGymQueryService _gymQueryService;

    public GetSessionPolicyQueryHandler(IGymQueryService gymQueryService)
    {
        _gymQueryService = gymQueryService;
    }

    public async Task<Result<SessionPolicyDto>> Handle(GetSessionPolicyQuery request, CancellationToken cancellationToken)
    {
        var result = await _gymQueryService.GetSessionPolicyAsync(new GymId(request.GymId), cancellationToken);
        return Result.Success(result);
    }
}
