using GymApp.Common.Application.Messaging;
using GymApp.Common.Domain;
using GymApp.Modules.Gyms.Application.Contracts;
using GymApp.Modules.Gyms.Application.DTOs;

namespace GymApp.Modules.Gyms.Application.Queries.GetGymTrainers;

internal sealed class GetGymTrainersQueryHandler : IQueryHandler<GetGymTrainersQuery, List<TrainerDto>>
{
    private readonly IGymQueryService _gymQueryService;

    public GetGymTrainersQueryHandler(IGymQueryService gymQueryService)
    {
        _gymQueryService = gymQueryService;
    }

    public async Task<Result<List<TrainerDto>>> Handle(GetGymTrainersQuery request, CancellationToken cancellationToken)
    {
        var result = await _gymQueryService.GetTrainersAsync(request.GymId, request.Status, cancellationToken);
        return Result.Success(result);
    }
}
