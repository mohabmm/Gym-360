using GymApp.Common.Application.Abstractions;
using GymApp.Modules.Gyms.IntegrationEvents;
using GymApp.Modules.Progress.Application.Abstractions;
using GymApp.Modules.Progress.Domain.Entities;
using GymApp.Modules.Progress.Domain.Enums;
using GymApp.Modules.Progress.Domain.Repositories;
using MediatR;
using Microsoft.Extensions.Logging;

namespace GymApp.Modules.Progress.Application.IntegrationEventHandlers;

internal sealed class DataImportInBodyIntegrationEventHandler : INotificationHandler<DataImportInBodyIntegrationEvent>
{
    private readonly IBodyMetricEntryRepository _repository;
    private readonly IGymMemberLookup _memberLookup;
    private readonly IProgressUnitOfWork _unitOfWork;
    private readonly ILogger<DataImportInBodyIntegrationEventHandler> _logger;

    public DataImportInBodyIntegrationEventHandler(
        IBodyMetricEntryRepository repository,
        IGymMemberLookup memberLookup,
        IProgressUnitOfWork unitOfWork,
        ILogger<DataImportInBodyIntegrationEventHandler> logger)
    {
        _repository = repository;
        _memberLookup = memberLookup;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task Handle(DataImportInBodyIntegrationEvent n, CancellationToken ct)
    {
        var memberId = await _memberLookup.GetUserIdByMemberNumberAsync(n.GymId, n.MemberNumber, ct);
        if (memberId is null)
        {
            _logger.LogWarning(
                "DataImport job {JobId} row {Row}: member '{MemberNumber}' not found — InBody row skipped",
                n.ImportJobId, n.RowNumber, n.MemberNumber);
            return;
        }

        var notes = BuildNotes(n);

        var result = BodyMetricEntry.Create(
            userId: memberId.Value,
            gymId: n.GymId,
            date: DateOnly.FromDateTime(n.MeasuredAt),
            weight: n.WeightKg,
            weightUnit: WeightUnit.Kg,
            bodyFatPercentage: n.BodyFatPercentage,
            measurements: null,
            source: MetricSource.InBodyScan,
            loggedBy: memberId.Value,
            notes: notes);

        if (result.IsFailure)
        {
            _logger.LogWarning(
                "DataImport job {JobId} row {Row}: InBody create failed: {Error}",
                n.ImportJobId, n.RowNumber, result.Error);
            return;
        }

        await _repository.AddAsync(result.Value, ct);
        await _unitOfWork.SaveChangesAsync(ct);
    }

    private static string? BuildNotes(DataImportInBodyIntegrationEvent n)
    {
        var parts = new List<string>();
        if (n.SkeletalMuscleMassKg.HasValue) parts.Add($"SMM: {n.SkeletalMuscleMassKg} kg");
        if (n.VisceralFatLevel.HasValue) parts.Add($"Visceral: {n.VisceralFatLevel}");
        if (n.Bmi.HasValue) parts.Add($"BMI: {n.Bmi}");
        if (n.BasalMetabolicRate.HasValue) parts.Add($"BMR: {n.BasalMetabolicRate}");
        if (n.BodyWaterKg.HasValue) parts.Add($"Water: {n.BodyWaterKg} kg");
        if (!string.IsNullOrWhiteSpace(n.Notes)) parts.Add(n.Notes);
        return parts.Count == 0 ? null : string.Join(" | ", parts);
    }
}
