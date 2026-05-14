using GymApp.Common.Application.Messaging;
using GymApp.Modules.Gyms.Domain.Enums;

namespace GymApp.Modules.Gyms.Application.Commands.StartDataImport;

public sealed record StartDataImportCommand(
    Guid GymId,
    Guid UploadedByUserId,
    DataImportSource Source,
    string OriginalFileName,
    long FileSizeBytes,
    Stream Content) : ICommand<Guid>;
