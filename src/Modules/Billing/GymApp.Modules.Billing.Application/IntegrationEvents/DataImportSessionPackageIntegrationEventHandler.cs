using GymApp.Modules.Identity.Shared;
using GymApp.Modules.Billing.Application.Abstractions;
using GymApp.Modules.Billing.Domain.Entities;
using GymApp.Modules.Billing.Domain.Repositories;
using GymApp.Modules.Billing.Domain.ValueObjects;
using GymApp.Modules.Gyms.IntegrationEvents;
using MediatR;
using Microsoft.Extensions.Logging;

namespace GymApp.Modules.Billing.Application.IntegrationEvents;

internal sealed class DataImportSessionPackageIntegrationEventHandler : INotificationHandler<DataImportSessionPackageIntegrationEvent>
{
    public static readonly Guid ImportedPackageTemplateId =
        Guid.Parse("FFFFFFFF-FFFF-FFFF-FFFF-FFFFFFFFFFFE");

    private readonly ISessionPackageRepository _packageRepository;
    private readonly IGymMemberLookup _memberLookup;
    private readonly IBillingUnitOfWork _unitOfWork;
    private readonly ILogger<DataImportSessionPackageIntegrationEventHandler> _logger;

    public DataImportSessionPackageIntegrationEventHandler(
        ISessionPackageRepository packageRepository,
        IGymMemberLookup memberLookup,
        IBillingUnitOfWork unitOfWork,
        ILogger<DataImportSessionPackageIntegrationEventHandler> logger)
    {
        _packageRepository = packageRepository;
        _memberLookup = memberLookup;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task Handle(DataImportSessionPackageIntegrationEvent n, CancellationToken ct)
    {
        var memberId = await _memberLookup.GetUserIdByMemberNumberAsync(n.GymId, n.MemberNumber, ct);
        if (memberId is null)
        {
            _logger.LogWarning(
                "DataImport job {JobId} row {Row}: member '{MemberNumber}' not found — package skipped",
                n.ImportJobId, n.RowNumber, n.MemberNumber);
            return;
        }

        Guid? trainerId = null;
        if (!string.IsNullOrWhiteSpace(n.TrainerMemberNumber))
        {
            trainerId = await _memberLookup.GetUserIdByMemberNumberAsync(n.GymId, n.TrainerMemberNumber, ct);
        }

        if (trainerId is null)
        {
            _logger.LogWarning(
                "DataImport job {JobId} row {Row}: trainer '{TrainerNumber}' unresolved — package skipped",
                n.ImportJobId, n.RowNumber, n.TrainerMemberNumber ?? "(null)");
            return;
        }

        var pricePer = Money.Create(
            n.TotalSessions > 0 ? Math.Round(n.TotalPrice / n.TotalSessions, 2) : 0m,
            n.Currency);
        var total = Money.Create(n.TotalPrice, n.Currency);

        var package = SessionPackage.Create(new SessionPackageDetails
        {
            GymId = n.GymId,
            TrainerId = trainerId.Value,
            MemberId = memberId.Value,
            TemplateId = ImportedPackageTemplateId,
            Name = n.PackageName,
            TotalSessions = n.TotalSessions,
            PricePerSession = pricePer,
            TotalPrice = total,
            AssignedBy = trainerId.Value,
            ExpiresAt = n.ExpiresAt
        });

        await _packageRepository.AddAsync(package, ct);
        await _unitOfWork.SaveChangesAsync(ct);
    }
}
