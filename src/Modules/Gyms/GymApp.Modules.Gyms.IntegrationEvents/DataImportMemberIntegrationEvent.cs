using GymApp.Common.Domain;

namespace GymApp.Modules.Gyms.IntegrationEvents;

public sealed record DataImportMemberIntegrationEvent(
    Guid ImportJobId,
    int RowNumber,
    Guid GymId,
    string MemberNumber,
    string FirstName,
    string LastName,
    string? Email,
    string Phone,
    DateTime? DateOfBirth,
    string Gender,
    DateTime JoinedAt,
    string? Section,
    string? NationalId) : IIntegrationEvent;
