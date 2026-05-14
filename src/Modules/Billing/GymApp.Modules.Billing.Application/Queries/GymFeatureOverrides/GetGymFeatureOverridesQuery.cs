using GymApp.Common.Application.Messaging;
using GymApp.Modules.Billing.Application.DTOs;

namespace GymApp.Modules.Billing.Application.Queries.GymFeatureOverrides;

public sealed record GetGymFeatureOverridesQuery(Guid GymId) : IQuery<List<GymFeatureOverrideDto>>;
