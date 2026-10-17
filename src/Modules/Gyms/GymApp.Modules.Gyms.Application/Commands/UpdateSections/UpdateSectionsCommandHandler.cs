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

        var requestedTypes = ParseSectionTypes(request.Sections);
        if (requestedTypes.IsFailure)
            return requestedTypes;

        var existing = await _sectionRepository.GetByGymIdAsync(request.GymId, cancellationToken);
        var existingByType = existing.ToDictionary(e => e.SectionType);

        foreach (var type in requestedTypes.Value)
            await ActivateOrCreateAsync(request.GymId, type, existingByType, cancellationToken);

        DeactivateUnrequested(existing, requestedTypes.Value);

        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }

    private static Result<HashSet<SectionType>> ParseSectionTypes(IEnumerable<string> sections)
    {
        var requestedTypes = new HashSet<SectionType>();
        foreach (var s in sections)
        {
            if (!Enum.TryParse<SectionType>(s, ignoreCase: true, out var type))
                return Result.Failure<HashSet<SectionType>>(new Error("Gyms.InvalidSectionType", $"Invalid section type: {s}"));
            requestedTypes.Add(type);
        }

        return Result.Success(requestedTypes);
    }

    private async Task ActivateOrCreateAsync(
        Guid gymId,
        SectionType type,
        Dictionary<SectionType, GymSection> existingByType,
        CancellationToken cancellationToken)
    {
        if (!existingByType.TryGetValue(type, out var section))
        {
            var created = GymSection.Create(gymId, type, name: null);
            await _sectionRepository.AddAsync(created, cancellationToken);
            return;
        }

        if (!section.IsActive) section.Activate();
    }

    private static void DeactivateUnrequested(IEnumerable<GymSection> existing, HashSet<SectionType> requestedTypes)
    {
        foreach (var section in existing.Where(s => s.IsActive && !requestedTypes.Contains(s.SectionType)))
            section.Deactivate();
    }
}
