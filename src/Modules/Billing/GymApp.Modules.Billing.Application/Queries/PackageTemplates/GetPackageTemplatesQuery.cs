using GymApp.Common.Application.Messaging;
using GymApp.Modules.Billing.Application.DTOs;

namespace GymApp.Modules.Billing.Application.Queries.PackageTemplates;

public sealed record GetPackageTemplatesQuery(Guid GymId) : IQuery<List<PackageTemplateDto>>;
