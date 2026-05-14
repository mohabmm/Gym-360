using GymApp.Common.Application.Messaging;

namespace GymApp.Modules.Gyms.Application.Queries.GetDataImportErrors;

public sealed record GetDataImportErrorsQuery(Guid GymId, Guid JobId) : IQuery<Stream>;
