using GymApp.Common.Application.Messaging;

namespace GymApp.Modules.Gyms.Application.Queries.VerifyKioskPin;

public sealed record VerifyKioskPinQuery(
    Guid GymId,
    string Pin) : IQuery<bool>;
