using GymApp.Common.Application.Messaging;
using GymApp.Modules.Gyms.Application.DTOs;

namespace GymApp.Modules.Gyms.Application.Queries.GetTrainerClients;

public sealed record GetTrainerClientsQuery(Guid GymId, Guid TrainerId) : IQuery<List<MemberDto>>;
