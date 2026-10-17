using GymApp.Common.Application.Messaging;
using GymApp.Common.Domain;
using GymApp.Modules.Gyms.Application.Contracts;
using GymApp.Modules.Gyms.Application.DTOs;
using GymApp.Modules.Gyms.Domain.Errors;
using GymApp.Modules.Gyms.Domain.ValueObjects;

namespace GymApp.Modules.Gyms.Application.Queries.GetGymAnalytics;

internal sealed class GetGymAnalyticsQueryHandler : IQueryHandler<GetGymAnalyticsQuery, GymAnalyticsResponse>
{
    private readonly IGymQueryService _gymQueryService;

    public GetGymAnalyticsQueryHandler(IGymQueryService gymQueryService)
    {
        _gymQueryService = gymQueryService;
    }

    public async Task<Result<GymAnalyticsResponse>> Handle(GetGymAnalyticsQuery request, CancellationToken cancellationToken)
    {
        var analytics = await _gymQueryService.GetAnalyticsAsync(new GymId(request.GymId), cancellationToken);

        if (analytics is null)
            return Result.Failure<GymAnalyticsResponse>(GymErrors.NotFound);

        return Result.Success(analytics);
    }
}
