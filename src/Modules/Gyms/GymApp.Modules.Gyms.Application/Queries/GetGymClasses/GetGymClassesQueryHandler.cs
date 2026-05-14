using GymApp.Common.Application.Messaging;
using GymApp.Common.Domain;
using GymApp.Modules.Gyms.Application.Contracts;
using GymApp.Modules.Gyms.Application.DTOs;

namespace GymApp.Modules.Gyms.Application.Queries.GetGymClasses;

internal sealed class GetGymClassesQueryHandler : IQueryHandler<GetGymClassesQuery, List<GymClassDto>>
{
    private readonly IGymQueryService _gymQueryService;

    public GetGymClassesQueryHandler(IGymQueryService gymQueryService)
    {
        _gymQueryService = gymQueryService;
    }

    public async Task<Result<List<GymClassDto>>> Handle(GetGymClassesQuery request, CancellationToken cancellationToken)
    {
        var result = await _gymQueryService.GetClassesAsync(request.GymId, cancellationToken);
        return Result.Success(result);
    }
}
