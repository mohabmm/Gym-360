using GymApp.Common.Application.Messaging;
using GymApp.Common.Domain;
using GymApp.Modules.Gyms.Application.Abstractions;
using GymApp.Modules.Gyms.Domain.Entities;
using GymApp.Modules.Gyms.Domain.Errors;
using GymApp.Modules.Gyms.Domain.Repositories;

namespace GymApp.Modules.Gyms.Application.Commands.UpdateSeasonalHours;

internal sealed class UpdateSeasonalHoursCommandHandler : ICommandHandler<UpdateSeasonalHoursCommand>
{
    private readonly IGymRepository _gymRepository;
    private readonly IGymSeasonalHoursRepository _seasonalRepository;
    private readonly IGymsUnitOfWork _unitOfWork;

    public UpdateSeasonalHoursCommandHandler(
        IGymRepository gymRepository,
        IGymSeasonalHoursRepository seasonalRepository,
        IGymsUnitOfWork unitOfWork)
    {
        _gymRepository = gymRepository;
        _seasonalRepository = seasonalRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result> Handle(UpdateSeasonalHoursCommand request, CancellationToken cancellationToken)
    {
        var gym = await _gymRepository.GetByIdAsync(request.GymId, cancellationToken);
        if (gym is null)
            return Result.Failure(GymErrors.NotFound);

        var existing = await _seasonalRepository.GetByGymIdAsync(request.GymId, cancellationToken);
        foreach (var item in existing)
            _seasonalRepository.Remove(item);

        foreach (var input in request.Overrides)
        {
            var details = SeasonalOverrideParser.Parse(input);
            if (details.IsFailure)
                return Result.Failure(details.Error);

            var entity = GymSeasonalHours.Create(request.GymId, details.Value);
            if (!input.IsActive) entity.Deactivate();
            await _seasonalRepository.AddAsync(entity, cancellationToken);
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}
