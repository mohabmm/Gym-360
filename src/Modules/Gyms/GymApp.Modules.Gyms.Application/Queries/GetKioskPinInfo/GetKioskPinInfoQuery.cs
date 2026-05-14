using GymApp.Common.Application.Messaging;
using GymApp.Modules.Gyms.Application.DTOs;

namespace GymApp.Modules.Gyms.Application.Queries.GetKioskPinInfo;

public sealed record GetKioskPinInfoQuery(Guid GymId) : IQuery<KioskPinInfoResponse>;
