using GymApp.Common.Application.Messaging;
using GymApp.Common.Domain;
using GymApp.Modules.Gyms.Application.Contracts;
using GymApp.Modules.Gyms.Application.DTOs;
using GymApp.Modules.Gyms.Domain.Errors;
using GymApp.Modules.Gyms.Domain.ValueObjects;

namespace GymApp.Modules.Gyms.Application.Queries.GetTrainerClients;

internal sealed class GetTrainerClientsQueryHandler : IQueryHandler<GetTrainerClientsQuery, List<MemberDto>>
{
    private readonly IGymQueryService _gymQueryService;

    public GetTrainerClientsQueryHandler(IGymQueryService gymQueryService)
    {
        _gymQueryService = gymQueryService;
    }

    public async Task<Result<List<MemberDto>>> Handle(GetTrainerClientsQuery request, CancellationToken cancellationToken)
    {
        var clients = await _gymQueryService.GetTrainerClientsAsync(new GymId(request.GymId), new TrainerId(request.TrainerId), cancellationToken);
        if (clients is null)
            return Result.Failure<List<MemberDto>>(GymErrors.TrainerNotFound);

        return Result.Success(clients);
    }
}
