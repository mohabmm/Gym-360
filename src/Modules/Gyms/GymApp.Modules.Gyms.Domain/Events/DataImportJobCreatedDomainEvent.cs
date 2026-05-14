using GymApp.Common.Domain;

namespace GymApp.Modules.Gyms.Domain.Events;

public sealed record DataImportJobCreatedDomainEvent(Guid JobId, Guid GymId) : IDomainEvent;
