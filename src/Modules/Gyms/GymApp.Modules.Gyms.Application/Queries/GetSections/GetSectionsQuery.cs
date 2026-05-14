using GymApp.Common.Application.Messaging;
using GymApp.Modules.Gyms.Application.DTOs;

namespace GymApp.Modules.Gyms.Application.Queries.GetSections;

public sealed record GetSectionsQuery(Guid GymId) : IQuery<List<GymSectionDto>>;
