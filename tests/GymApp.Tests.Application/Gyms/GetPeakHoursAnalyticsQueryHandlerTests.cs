using GymApp.Common.Application.Abstractions;
using GymApp.Modules.Gyms.Application.Contracts;
using GymApp.Modules.Gyms.Application.DTOs;
using GymApp.Modules.Gyms.Application.Queries.GetPeakHoursAnalytics;
using GymApp.Modules.Gyms.Domain.Errors;

namespace GymApp.Tests.Application.Gyms;

public class GetPeakHoursAnalyticsQueryHandlerTests
{
    private readonly IGymQueryService _queryService = Substitute.For<IGymQueryService>();
    private readonly IGymPlanService _planService = Substitute.For<IGymPlanService>();
    private readonly GetPeakHoursAnalyticsQueryHandler _sut;

    public GetPeakHoursAnalyticsQueryHandlerTests()
    {
        _sut = new GetPeakHoursAnalyticsQueryHandler(_queryService, _planService);
    }

    [Fact]
    public async Task Handle_FeatureDisabled_ReturnsFeatureNotAvailable()
    {
        var gymId = Guid.NewGuid();
        _planService.IsFeatureEnabledAsync(gymId, GetPeakHoursAnalyticsQueryHandler.FeatureCode, Arg.Any<CancellationToken>())
            .Returns(false);
        _planService.GetPlanCodeAsync(gymId, Arg.Any<CancellationToken>())
            .Returns("Starter");

        var query = new GetPeakHoursAnalyticsQuery(gymId, null, null, 180);

        var result = await _sut.Handle(query, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("PLAN.FEATURE_NOT_AVAILABLE");
        await _queryService.DidNotReceive().GetPeakHoursAsync(
            Arg.Any<Guid>(), Arg.Any<DateTime>(), Arg.Any<DateTime>(), Arg.Any<int>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_FeatureEnabled_ReturnsBucketsFromQueryService()
    {
        var gymId = Guid.NewGuid();
        _planService.IsFeatureEnabledAsync(gymId, GetPeakHoursAnalyticsQueryHandler.FeatureCode, Arg.Any<CancellationToken>())
            .Returns(true);

        var expected = new PeakHoursAnalyticsResponse(
            gymId,
            From: DateTime.UtcNow.AddDays(-30),
            To: DateTime.UtcNow,
            TimezoneOffsetMinutes: 180,
            TotalCheckIns: 3,
            Buckets: new[] { new PeakHourBucket(DayOfWeek: 1, Hour: 19, Count: 3) });
        _queryService.GetPeakHoursAsync(gymId, Arg.Any<DateTime>(), Arg.Any<DateTime>(), 180, Arg.Any<CancellationToken>())
            .Returns(expected);

        var query = new GetPeakHoursAnalyticsQuery(gymId, null, null, 180);

        var result = await _sut.Handle(query, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.TotalCheckIns.Should().Be(3);
        result.Value.Buckets.Should().ContainSingle(b => b.Hour == 19 && b.Count == 3);
    }

    [Fact]
    public async Task Handle_GymNotFound_ReturnsNotFound()
    {
        var gymId = Guid.NewGuid();
        _planService.IsFeatureEnabledAsync(gymId, GetPeakHoursAnalyticsQueryHandler.FeatureCode, Arg.Any<CancellationToken>())
            .Returns(true);
        _queryService.GetPeakHoursAsync(gymId, Arg.Any<DateTime>(), Arg.Any<DateTime>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns((PeakHoursAnalyticsResponse?)null);

        var query = new GetPeakHoursAnalyticsQuery(gymId, null, null, 180);

        var result = await _sut.Handle(query, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be(GymErrors.NotFound.Code);
    }
}
