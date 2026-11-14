using GymApp.Common.Domain;
using GymApp.Modules.Billing.Shared;
using GymApp.Modules.Progress.Application.Abstractions;
using GymApp.Modules.Progress.Application.Commands.LogInBodyScan;
using GymApp.Modules.Progress.Application.Mappers;
using GymApp.Modules.Progress.Domain;
using GymApp.Modules.Progress.Domain.Entities;
using GymApp.Modules.Progress.Domain.Enums;
using GymApp.Modules.Progress.Domain.Repositories;
using GymApp.Modules.Progress.Domain.ValueObjects;

namespace GymApp.Tests.Application.Progress;

/// <summary>progress.inbody: member saves the values read from an InBody sheet.</summary>
public sealed class LogInBodyScanCommandHandlerTests
{
    private static readonly DateOnly Today = new(2026, 11, 14);

    private readonly Guid _member = Guid.NewGuid();
    private readonly Guid _gym = Guid.NewGuid();

    private readonly IBodyMetricEntryRepository _metrics = Substitute.For<IBodyMetricEntryRepository>();
    private readonly IProgressUnitOfWork _unitOfWork = Substitute.For<IProgressUnitOfWork>();
    private readonly IGymPlanService _plan = Substitute.For<IGymPlanService>();

    public LogInBodyScanCommandHandlerTests()
    {
        _plan.IsFeatureEnabledAsync(_gym, "inbody_integration", Arg.Any<CancellationToken>()).Returns(true);
    }

    private LogInBodyScanCommandHandler Handler() => new(_metrics, _unitOfWork, _plan);

    private LogInBodyScanCommand Scan(decimal? weight = 80m, decimal? height = 178m) =>
        new(_member, _gym, Today, new InBodyScanValues(weight, height, 18.5m, 35m, 45m), null);

    [Fact]
    public async Task Gym_without_the_feature_is_refused_before_any_work()
    {
        _plan.IsFeatureEnabledAsync(_gym, "inbody_integration", Arg.Any<CancellationToken>()).Returns(false);
        _plan.GetPlanCodeAsync(_gym, Arg.Any<CancellationToken>()).Returns("pro");

        var result = await Handler().Handle(Scan(), default);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Contain("FEATURE");
        await _metrics.DidNotReceiveWithAnyArgs().GetByUserAndDateAsync(default, default, default, default);
        await _unitOfWork.DidNotReceiveWithAnyArgs().SaveChangesAsync(default);
    }

    [Fact]
    public async Task First_scan_of_the_day_creates_an_InBody_entry()
    {
        BodyMetricEntry? saved = null;
        await _metrics.AddAsync(Arg.Do<BodyMetricEntry>(e => saved = e), Arg.Any<CancellationToken>());

        var result = await Handler().Handle(Scan(), default);

        result.IsSuccess.Should().BeTrue();
        result.Value.Created.Should().BeTrue();
        saved!.Source.Should().Be(MetricSource.InBodyScan);
        saved.Weight.Should().Be(80m);
        saved.BodyFatPercentage.Should().Be(18.5m);
        saved.Composition.Should().Be(new BodyComposition { HeightCm = 178m, SkeletalMuscleMassKg = 35m, BodyWaterKg = 45m });
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Scan_on_a_day_already_logged_updates_that_entry_and_keeps_tape()
    {
        var tape = new BodyMeasurements { Waist = 90m, MeasurementUnit = MeasurementUnit.Cm };
        var existing = BodyMetricEntry.Create(new BodyMetricEntryDetails
        {
            UserId = _member,
            GymId = _gym,
            Date = Today,
            Source = MetricSource.SelfReported,
            LoggedBy = _member,
            Reading = new BodyMetricReading { Weight = 82m, WeightUnit = WeightUnit.Kg, Measurements = tape },
        }).Value;
        _metrics.GetByUserAndDateAsync(_member, _gym, Today, Arg.Any<CancellationToken>()).Returns(existing);

        var result = await Handler().Handle(Scan(), default);

        result.Value.Should().Be(new LogInBodyScanResult(existing.Id, Created: false));
        existing.Weight.Should().Be(80m);
        existing.Measurements.Should().Be(tape);
        existing.Source.Should().Be(MetricSource.InBodyScan);
        _metrics.Received(1).Update(existing);
        await _metrics.DidNotReceiveWithAnyArgs().AddAsync(default!, default);
    }

    [Fact]
    public async Task Invalid_values_are_not_saved()
    {
        var result = await Handler().Handle(Scan(height: 300m), default);

        result.Error.Should().Be(ProgressErrors.InvalidHeight);
        await _unitOfWork.DidNotReceiveWithAnyArgs().SaveChangesAsync(default);
    }

    [Fact]
    public void Dto_shows_muscle_and_water_as_share_of_weight_and_bmi()
    {
        var entry = BodyMetricEntry.Create(new BodyMetricEntryDetails
        {
            UserId = _member,
            GymId = _gym,
            Date = Today,
            Source = MetricSource.InBodyScan,
            LoggedBy = _member,
            Reading = new BodyMetricReading
            {
                Weight = 80m,
                WeightUnit = WeightUnit.Kg,
                Composition = new BodyComposition { HeightCm = 178m, SkeletalMuscleMassKg = 35m, BodyWaterKg = 45m },
            },
        }).Value;

        var dto = ProgressMapper.ToDto(entry).Composition!;

        dto.SkeletalMusclePercent.Should().Be(43.8m);
        dto.BodyWaterPercent.Should().Be(56.3m);
        dto.Bmi.Should().Be(25.2m);
    }

    [Fact]
    public void Validator_rejects_a_scan_with_no_values()
    {
        var empty = new LogInBodyScanCommand(_member, _gym, Today, new InBodyScanValues(null, null, null, null, null), null);

        new LogInBodyScanCommandValidator().Validate(empty).IsValid.Should().BeFalse();
        new LogInBodyScanCommandValidator().Validate(Scan()).IsValid.Should().BeTrue();
    }
}
