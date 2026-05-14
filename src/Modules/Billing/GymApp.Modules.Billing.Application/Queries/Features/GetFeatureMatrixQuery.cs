using GymApp.Common.Application.Messaging;
using GymApp.Modules.Billing.Application.DTOs;

namespace GymApp.Modules.Billing.Application.Queries.Features;

public sealed record GetFeatureMatrixQuery() : IQuery<FeatureMatrixResponse>;
