using GymApp.Modules.Billing.Shared;
using GymApp.Common.Application.Errors;
using GymApp.Common.Application.Messaging;
using GymApp.Common.Domain;
using GymApp.Modules.Progress.Domain;
using GymApp.Modules.Progress.Domain.Entities;
using GymApp.Modules.Progress.Domain.Enums;
using GymApp.Modules.Progress.Domain.Repositories;
using GymApp.Modules.Progress.Application.Abstractions;
using GymApp.Modules.Progress.Domain.ValueObjects;

namespace GymApp.Modules.Progress.Application.Commands.UploadProgressPhoto;

internal sealed class UploadProgressPhotoCommandHandler : ICommandHandler<UploadProgressPhotoCommand, Guid>
{
    private readonly IProgressPhotoRepository _repository;
    private readonly IProgressUnitOfWork _unitOfWork;
    private readonly IGymPlanService _planService;

    public UploadProgressPhotoCommandHandler(
        IProgressPhotoRepository repository,
        IProgressUnitOfWork unitOfWork,
        IGymPlanService planService)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
        _planService = planService;
    }

    public async Task<Result<Guid>> Handle(UploadProgressPhotoCommand request, CancellationToken cancellationToken)
    {
        bool isEnabled = await _planService.IsFeatureEnabledAsync(
            request.GymId, "progress_photos", cancellationToken);

        if (!isEnabled)
        {
            var currentPlan = await _planService.GetPlanCodeAsync(request.GymId, cancellationToken);
            return Result.Failure<Guid>(
                PlanErrors.FeatureNotAvailable("progress_photos", currentPlan, "Pro"));
        }

        int photoLimit = await _planService.GetPhotoLimitPerMemberAsync(request.GymId, cancellationToken);
        int currentCount = await _repository.GetCountAsync(request.UserId, request.GymId, cancellationToken);

        if (photoLimit > 0 && currentCount >= photoLimit)
        {
            return Result.Failure<Guid>(ProgressErrors.PhotoLimitReached);
        }

        if (!Enum.TryParse<PhotoAngle>(request.Angle, true, out var angle))
            angle = PhotoAngle.Front;

        var result = ProgressPhoto.Create(
            request.UserId,
            request.GymId,
            new ProgressPhotoDetails
            {
                PhotoUrl = request.PhotoUrl,
                ThumbnailUrl = request.ThumbnailUrl,
                Angle = angle,
                Date = request.Date,
                Notes = request.Notes
            });

        if (result.IsFailure)
        {
            return Result.Failure<Guid>(result.Error);
        }

        await _repository.AddAsync(result.Value, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success(result.Value.Id);
    }
}
