using GymApp.Common.Application.Messaging;
using GymApp.Modules.Identity.Application.DTOs;

namespace GymApp.Modules.Identity.Application.Commands.ExportMyData;

public sealed record ExportMyDataCommand(Guid UserId) : ICommand<DataExportResponse>;
