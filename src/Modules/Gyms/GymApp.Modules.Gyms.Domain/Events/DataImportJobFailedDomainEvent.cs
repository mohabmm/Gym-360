using GymApp.Common.Domain;

namespace GymApp.Modules.Gyms.Domain.Events;

public sealed record DataImportJobFailedDomainEvent(Guid JobId, Guid GymId, string Reason) : IDomainEvent;
