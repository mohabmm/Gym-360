using GymApp.Common.Application.Messaging;
using GymApp.Common.Domain;
using GymApp.Modules.Identity.Application.Abstractions;
using GymApp.Modules.Identity.Domain.Entities;

namespace GymApp.Modules.Identity.Application.Commands.PlatformSettings;

internal sealed class UpdatePlatformSettingsCommandHandler : ICommandHandler<UpdatePlatformSettingsCommand>
{
    private readonly IPlatformSettingsRepository _repository;
    private readonly IIdentityUnitOfWork _unitOfWork;

    public UpdatePlatformSettingsCommandHandler(
        IPlatformSettingsRepository repository,
        IIdentityUnitOfWork unitOfWork)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result> Handle(UpdatePlatformSettingsCommand request, CancellationToken cancellationToken)
    {
        foreach (var (key, value) in request.Values)
        {
            if (string.IsNullOrWhiteSpace(key))
                continue;

            var existing = await _repository.GetByKeyAsync(key, cancellationToken);
            if (existing is null)
            {
                var created = PlatformSetting.Create(key, value, request.UpdatedBy);
                await _repository.AddAsync(created, cancellationToken);
            }
            else
            {
                existing.SetValue(value, request.UpdatedBy);
                _repository.Update(existing);
            }
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}
