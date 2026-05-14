using GymApp.Common.Application.Messaging;

namespace GymApp.Modules.Billing.Application.Commands.Payments;

public sealed record VoidPaymentCommand(
    Guid PaymentId,
    string Reason) : ICommand;
