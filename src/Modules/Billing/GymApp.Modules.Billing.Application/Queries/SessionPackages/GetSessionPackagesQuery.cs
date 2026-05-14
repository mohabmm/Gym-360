using GymApp.Common.Application.Messaging;
using GymApp.Modules.Billing.Application.DTOs;

namespace GymApp.Modules.Billing.Application.Queries.SessionPackages;

public sealed record GetSessionPackagesQuery(
    Guid GymId,
    Guid? TrainerId,
    Guid? MemberId,
    string? Status) : IQuery<List<SessionPackageDto>>;
