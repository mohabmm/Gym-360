using GymApp.Common.Application.Messaging;
using GymApp.Common.Domain;
using GymApp.Modules.Gyms.Application.Contracts;
using GymApp.Modules.Gyms.Application.DTOs;
using GymApp.Modules.Gyms.Domain.Errors;

namespace GymApp.Modules.Gyms.Application.Queries.GetGym;

internal sealed class GetGymQueryHandler : IQueryHandler<GetGymQuery, GymResponse>
{
    private readonly IGymQueryService _gymQueryService;

    public GetGymQueryHandler(IGymQueryService gymQueryService)
    {
        _gymQueryService = gymQueryService;
    }

    public async Task<Result<GymResponse>> Handle(GetGymQuery request, CancellationToken cancellationToken)
    {
        var gym = await _gymQueryService.GetGymByIdAsync(request.GymId, cancellationToken);

        if (gym is null)
            return Result.Failure<GymResponse>(GymErrors.NotFound);

        return Result.Success(gym);
    }
}
