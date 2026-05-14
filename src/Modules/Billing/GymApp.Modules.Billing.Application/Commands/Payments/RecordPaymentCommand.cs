using GymApp.Common.Application.Messaging;

namespace GymApp.Modules.Billing.Application.Commands.Payments;

public sealed record RecordPaymentCommand(
    Guid GymId,
    Guid MemberId,
    decimal Amount,
    string PaymentMethod,
    string Purpose,
    Guid? ReferenceId,
    Guid RecordedBy,
    // Bidirectional flow: when Purpose=SessionPackage and ReferenceId is null,
    // supply TrainerId + SessionCount to atomically create the SessionPackage
    // and link the resulting payment.ReferenceId to it. Lets the staff
    // "Record Payment" screen double as an "Assign Trainer + Charge" flow.
    Guid? TrainerId = null,
    int? SessionCount = null) : ICommand<Guid>;
