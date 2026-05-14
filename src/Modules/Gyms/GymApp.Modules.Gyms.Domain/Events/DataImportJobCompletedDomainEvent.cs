using GymApp.Common.Domain;

namespace GymApp.Modules.Gyms.Domain.Events;

public sealed record DataImportJobCompletedDomainEvent(
    Guid JobId,
    Guid GymId,
    int SuccessCount,
    int FailureCount) : IDomainEvent;
