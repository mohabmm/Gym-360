using GymApp.Common.Application.Messaging;
using GymApp.Common.Domain;
using GymApp.Modules.Gyms.Application.Abstractions;
using GymApp.Modules.Gyms.Domain.Entities;
using GymApp.Modules.Gyms.Domain.Enums;
using GymApp.Modules.Gyms.Domain.Errors;
using GymApp.Modules.Gyms.Domain.Repositories;

namespace GymApp.Modules.Gyms.Application.Commands.UpdateSections;

internal sealed class UpdateSectionsCommandHandler : ICommandHandler<UpdateSectionsCommand>
{
    private readonly IGymRepository _gymRepository;
    private readonly IGymSectionRepository _sectionRepository;
    private readonly IGymsUnitOfWork _unitOfWork;

    public UpdateSectionsCommandHandler(
        IGymRepository gymRepository,
        IGymSectionRepository sectionRepository,
        IGymsUnitOfWork unitOfWork)
    {
        _gymRepository = gymRepository;
        _sectionRepository = sectionRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result> Handle(UpdateSectionsCommand request, CancellationToken cancellationToken)
    {
        var gym = await _gymRepository.GetByIdAsync(request.GymId, cancellationToken);
        if (gym is null)
            return Result.Failure(GymErrors.NotFound);

        var requestedTypes = new HashSet<SectionType>();
        foreach (var s in request.Sections)
        {
            if (!Enum.TryParse<SectionType>(s, ignoreCase: true, out var type))
                return Result.Failure(new Error("Gyms.InvalidSectionType", $"Invalid section type: {s}"));
            requestedTypes.Add(type);
        }

        var existing = await _sectionRepository.GetByGymIdAsync(request.GymId, cancellationToken);
        var existingByType = existing.ToDictionary(e => e.SectionType);

        foreach (var type in requestedTypes)
        {
            if (existingByType.TryGetValue(type, out var section))
            {
                if (!section.IsActive) section.Activate();
            }
            else
            {
                var created = GymSection.Create(request.GymId, type, name: null);
                await _sectionRepository.AddAsync(created, cancellationToken);
            }
        }

        foreach (var section in existing)
        {
            if (!requestedTypes.Contains(section.SectionType) && section.IsActive)
                section.Deactivate();
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}
