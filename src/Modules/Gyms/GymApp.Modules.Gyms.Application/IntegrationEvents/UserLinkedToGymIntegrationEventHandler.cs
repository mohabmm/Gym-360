using GymApp.Modules.Gyms.Application.Abstractions;
using GymApp.Modules.Gyms.Domain.Repositories;
using GymApp.Modules.Identity.IntegrationEvents;
using MediatR;
using Microsoft.Extensions.Logging;

namespace GymApp.Modules.Gyms.Application.IntegrationEvents;

// Keeps the Gym aggregate's TrainerIds in sync when a trainer is linked to a
// gym via an Identity-side path (self-serve LinkToGym, staff-create-trainer).
// Without this, GetTrainersAsync (which reads gym.TrainerIds) returned empty
// for those trainers, so they were invisible to staff (no Trainers list,
// empty class-create / assign-package dropdowns, empty coach leaderboard).
//
// This calls gym.AddTrainer directly (not the AddTrainer command), so it does
// not re-publish to Identity — no event loop. It is idempotent: AddTrainer
// returns TrainerAlreadyAdded if the id is already present, which we ignore.
internal sealed class UserLinkedToGymIntegrationEventHandler
    : INotificationHandler<UserLinkedToGymIntegrationEvent>
{
    private const string TrainerRole = "Trainer";

    private readonly IGymRepository _gymRepository;
    private readonly IGymsUnitOfWork _unitOfWork;
    private readonly ILogger<UserLinkedToGymIntegrationEventHandler> _logger;

    public UserLinkedToGymIntegrationEventHandler(
        IGymRepository gymRepository,
        IGymsUnitOfWork unitOfWork,
        ILogger<UserLinkedToGymIntegrationEventHandler> logger)
    {
        _gymRepository = gymRepository;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task Handle(UserLinkedToGymIntegrationEvent n, CancellationToken ct)
    {
        if (!string.Equals(n.Role, TrainerRole, StringComparison.OrdinalIgnoreCase))
            return;

        var gym = await _gymRepository.GetByIdAsync(n.GymId, ct);
        if (gym is null)
        {
            _logger.LogWarning(
                "UserLinkedToGym: gym {GymId} not found while adding trainer {UserId}",
                n.GymId, n.UserId);
            return;
        }

        var result = gym.AddTrainer(n.UserId);
        if (result.IsFailure)
            // Already added (or gym not active) — nothing to do.
            return;

        _gymRepository.Update(gym);
        await _unitOfWork.SaveChangesAsync(ct);
    }
}
