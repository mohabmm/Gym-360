using GymApp.Common.Application.Messaging;
using GymApp.Common.Domain;
using GymApp.Modules.Gamification.Application.DTOs;
using GymApp.Modules.Gamification.Domain.Repositories;

namespace GymApp.Modules.Gamification.Application.Queries.GetTrainerRatings;

internal sealed class GetTrainerRatingsQueryHandler : IQueryHandler<GetTrainerRatingsQuery, List<TrainerRatingDto>>
{
    private readonly ITrainerRatingRepository _ratingRepository;

    public GetTrainerRatingsQueryHandler(ITrainerRatingRepository ratingRepository)
    {
        _ratingRepository = ratingRepository;
    }

    public async Task<Result<List<TrainerRatingDto>>> Handle(
        GetTrainerRatingsQuery request,
        CancellationToken cancellationToken)
    {
        var fromDate = request.From?.ToDateTime(TimeOnly.MinValue) ?? DateTime.MinValue;
        var toDate = request.To?.ToDateTime(TimeOnly.MaxValue) ?? DateTime.MaxValue;

        var ratings = await _ratingRepository.GetByTrainerAsync(
            request.GymId, request.TrainerId, fromDate, toDate, cancellationToken);

        var dtos = ratings
            .Select(r => new TrainerRatingDto(
                r.Id,
                r.TrainerId,
                r.ClientId,
                r.SessionId,
                r.Stars,
                r.Comment,
                r.CreatedAt))
            .ToList();

        return Result.Success(dtos);
    }
}
