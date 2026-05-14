using GymApp.Common.Application.Messaging;
using GymApp.Modules.Gyms.Application.DTOs;

namespace GymApp.Modules.Gyms.Application.Queries.GetGymBranding;

public sealed record GetGymBrandingQuery(Guid GymId) : IQuery<BrandingResponse>;
