using GymApp.Common.Application.Messaging;
using GymApp.Modules.Gyms.Application.DTOs;

namespace GymApp.Modules.Gyms.Application.Queries.ListPendingGyms;

public sealed record ListPendingGymsQuery : IQuery<List<GymResponse>>;
