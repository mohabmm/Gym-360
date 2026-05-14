using GymApp.Common.Application.Messaging;
using GymApp.Modules.Gyms.Application.DTOs;

namespace GymApp.Modules.Gyms.Application.Queries.GetDataImportJob;

public sealed record GetDataImportJobQuery(Guid GymId, Guid JobId) : IQuery<DataImportJobDto>;
