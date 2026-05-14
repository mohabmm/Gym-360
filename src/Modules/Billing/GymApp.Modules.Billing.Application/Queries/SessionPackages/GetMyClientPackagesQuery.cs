using GymApp.Common.Application.Messaging;
using GymApp.Modules.Billing.Application.DTOs;

namespace GymApp.Modules.Billing.Application.Queries.SessionPackages;

public sealed record GetMyClientPackagesQuery(
    Guid TrainerId,
    Guid GymId) : IQuery<List<SessionPackageDto>>;
