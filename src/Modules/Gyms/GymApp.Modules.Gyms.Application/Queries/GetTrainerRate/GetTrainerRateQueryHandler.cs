using GymApp.Common.Application.Messaging;
using GymApp.Common.Domain;
using GymApp.Modules.Gyms.Application.DTOs;
using GymApp.Modules.Gyms.Domain.Errors;
using GymApp.Modules.Gyms.Domain.Repositories;

namespace GymApp.Modules.Gyms.Application.Queries.GetTrainerRate;

internal sealed class GetTrainerRateQueryHandler : IQueryHandler<GetTrainerRateQuery, TrainerRateDto>
{
    private readonly ITrainerRateRepository _trainerRateRepository;

    public GetTrainerRateQueryHandler(ITrainerRateRepository trainerRateRepository)
    {
        _trainerRateRepository = trainerRateRepository;
    }

    public async Task<Result<TrainerRateDto>> Handle(GetTrainerRateQuery request, CancellationToken cancellationToken)
    {
        var rate = await _trainerRateRepository.GetByTrainerAsync(
            request.GymId, request.TrainerId, cancellationToken);

        if (rate is null)
            return Result.Failure<TrainerRateDto>(GymErrors.TrainerRateNotFound);

        var dto = new TrainerRateDto(
            rate.GymId,
            rate.TrainerId,
            rate.RatePerSession,
            rate.Currency,
            rate.EffectiveFrom);

        return Result.Success(dto);
    }
}
