using GymApp.Common.Application.Messaging;
using GymApp.Common.Domain;
using GymApp.Modules.Billing.Application.Abstractions;
using GymApp.Modules.Billing.Application.DTOs;

namespace GymApp.Modules.Billing.Application.Queries.Analytics;

internal sealed class GetPlatformAnalyticsQueryHandler
    : IQueryHandler<GetPlatformAnalyticsQuery, PlatformAnalyticsDto>
{
    private readonly IPlatformAnalyticsReader _reader;

    public GetPlatformAnalyticsQueryHandler(IPlatformAnalyticsReader reader)
    {
        _reader = reader;
    }

    public async Task<Result<PlatformAnalyticsDto>> Handle(GetPlatformAnalyticsQuery request, CancellationToken cancellationToken)
    {
        var dto = await _reader.GetPlatformAnalyticsAsync(cancellationToken);
        return Result.Success(dto);
    }
}

internal sealed class GetChurnAnalyticsQueryHandler
    : IQueryHandler<GetChurnAnalyticsQuery, ChurnAnalyticsDto>
{
    private readonly IPlatformAnalyticsReader _reader;

    public GetChurnAnalyticsQueryHandler(IPlatformAnalyticsReader reader)
    {
        _reader = reader;
    }

    public async Task<Result<ChurnAnalyticsDto>> Handle(GetChurnAnalyticsQuery request, CancellationToken cancellationToken)
    {
        var dto = await _reader.GetChurnAnalyticsAsync(cancellationToken);
        return Result.Success(dto);
    }
}
