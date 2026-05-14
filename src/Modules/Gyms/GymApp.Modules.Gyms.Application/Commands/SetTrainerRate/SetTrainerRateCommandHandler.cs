using GymApp.Common.Application.Messaging;
using GymApp.Common.Domain;
using GymApp.Modules.Gyms.Domain.Entities;
using GymApp.Modules.Gyms.Domain.Errors;
using GymApp.Modules.Gyms.Domain.Repositories;
using GymApp.Modules.Gyms.Application.Abstractions;

namespace GymApp.Modules.Gyms.Application.Commands.SetTrainerRate;

internal sealed class SetTrainerRateCommandHandler : ICommandHandler<SetTrainerRateCommand>
{
    private readonly IGymRepository _gymRepository;
    private readonly ITrainerRateRepository _trainerRateRepository;
    private readonly IGymsUnitOfWork _unitOfWork;

    public SetTrainerRateCommandHandler(
        IGymRepository gymRepository,
        ITrainerRateRepository trainerRateRepository,
        IGymsUnitOfWork unitOfWork)
    {
        _gymRepository = gymRepository;
        _trainerRateRepository = trainerRateRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result> Handle(SetTrainerRateCommand request, CancellationToken cancellationToken)
    {
        var gym = await _gymRepository.GetByIdAsync(request.GymId, cancellationToken);
        if (gym is null)
            return Result.Failure(GymErrors.NotFound);

        var existing = await _trainerRateRepository.GetByTrainerAsync(
            request.GymId, request.TrainerId, cancellationToken);

        if (existing is null)
        {
            var rate = TrainerRate.Create(request.GymId, request.TrainerId, request.RatePerSession, request.SetByUserId);
            await _trainerRateRepository.AddAsync(rate, cancellationToken);
        }
        else
        {
            existing.Update(request.RatePerSession, request.SetByUserId);
            _trainerRateRepository.Update(existing);
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}
