using GymApp.Common.Application.Messaging;
using GymApp.Modules.Gyms.Application.DTOs;

namespace GymApp.Modules.Gyms.Application.Queries.ListDataImportJobs;

public sealed record ListDataImportJobsQuery(Guid GymId, int Skip, int Take) : IQuery<IReadOnlyList<DataImportJobDto>>;
