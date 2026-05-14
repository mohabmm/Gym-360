using GymApp.Common.Application.Messaging;
using GymApp.Common.Domain;
using GymApp.Modules.Identity.Application.Contracts;
using GymApp.Modules.Identity.Application.DTOs;
using GymApp.Modules.Identity.Domain.Errors;
using GymApp.Modules.Identity.Domain.Repositories;

namespace GymApp.Modules.Identity.Application.Commands.ExportMyData;

internal sealed class ExportMyDataCommandHandler : ICommandHandler<ExportMyDataCommand, DataExportResponse>
{
    private readonly IUserRepository _userRepository;
    private readonly IDataExportService _dataExportService;

    public ExportMyDataCommandHandler(
        IUserRepository userRepository,
        IDataExportService dataExportService)
    {
        _userRepository = userRepository;
        _dataExportService = dataExportService;
    }

    public async Task<Result<DataExportResponse>> Handle(ExportMyDataCommand request, CancellationToken cancellationToken)
    {
        var user = await _userRepository.GetByIdAsync(request.UserId, cancellationToken);
        if (user is null)
            return Result.Failure<DataExportResponse>(UserErrors.NotFound(request.UserId));

        var exportResponse = await _dataExportService.ExportUserDataAsync(request.UserId, cancellationToken);

        return Result.Success(exportResponse);
    }
}
