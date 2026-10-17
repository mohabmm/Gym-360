using GymApp.Common.Application.Messaging;
using GymApp.Common.Domain;
using GymApp.Modules.Gyms.Application.Contracts;
using GymApp.Modules.Gyms.Application.DTOs;
using GymApp.Modules.Gyms.Domain.Errors;
using GymApp.Modules.Gyms.Domain.ValueObjects;

namespace GymApp.Modules.Gyms.Application.Queries.GetGymClassById;

internal sealed class GetGymClassByIdQueryHandler : IQueryHandler<GetGymClassByIdQuery, GymClassDto>
{
    private readonly IGymQueryService _gymQueryService;

    public GetGymClassByIdQueryHandler(IGymQueryService gymQueryService)
    {
        _gymQueryService = gymQueryService;
    }

    public async Task<Result<GymClassDto>> Handle(GetGymClassByIdQuery request, CancellationToken cancellationToken)
    {
        var gymClass = await _gymQueryService.GetClassByIdAsync(new GymId(request.GymId), request.ClassId, cancellationToken);

        if (gymClass is null)
            return Result.Failure<GymClassDto>(GymErrors.ClassNotFound);

        return Result.Success(gymClass);
    }
}
