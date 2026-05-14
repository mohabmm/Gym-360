using GymApp.Common.Domain;

namespace GymApp.Modules.Gyms.IntegrationEvents;

public sealed record DataImportPaymentIntegrationEvent(
    Guid ImportJobId,
    int RowNumber,
    Guid GymId,
    string MemberNumber,
    decimal Amount,
    string Currency,
    string PaymentMethod,
    string Purpose,
    string ReceiptNumber,
    DateTime PaidAt) : IDomainEvent;
