using GymApp.Common.Application.Messaging;
using GymApp.Common.Domain;
using GymApp.Modules.Gyms.Application.Contracts;
using GymApp.Modules.Gyms.Application.DTOs;
using GymApp.Modules.Gyms.Domain.Errors;

namespace GymApp.Modules.Gyms.Application.Queries.GetGymTrainerById;

internal sealed class GetGymTrainerByIdQueryHandler : IQueryHandler<GetGymTrainerByIdQuery, TrainerDto>
{
    private readonly IGymQueryService _gymQueryService;

    public GetGymTrainerByIdQueryHandler(IGymQueryService gymQueryService)
    {
        _gymQueryService = gymQueryService;
    }

    public async Task<Result<TrainerDto>> Handle(GetGymTrainerByIdQuery request, CancellationToken cancellationToken)
    {
        var trainer = await _gymQueryService.GetTrainerByIdAsync(request.GymId, request.TrainerId, cancellationToken);

        if (trainer is null)
            return Result.Failure<TrainerDto>(GymErrors.TrainerNotFound);

        return Result.Success(trainer);
    }
}
