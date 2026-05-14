using GymApp.Common.Application.Messaging;
using GymApp.Common.Domain;
using GymApp.Modules.Gyms.Application.DTOs;
using GymApp.Modules.Gyms.Domain.Repositories;

namespace GymApp.Modules.Gyms.Application.Queries.GetSections;

internal sealed class GetSectionsQueryHandler : IQueryHandler<GetSectionsQuery, List<GymSectionDto>>
{
    private readonly IGymSectionRepository _sectionRepository;

    public GetSectionsQueryHandler(IGymSectionRepository sectionRepository)
    {
        _sectionRepository = sectionRepository;
    }

    public async Task<Result<List<GymSectionDto>>> Handle(GetSectionsQuery request, CancellationToken cancellationToken)
    {
        var sections = await _sectionRepository.GetByGymIdAsync(request.GymId, cancellationToken);
        var dtos = sections
            .Select(s => new GymSectionDto(s.Id, s.SectionType.ToString(), s.Name, s.IsActive))
            .ToList();
        return Result.Success(dtos);
    }
}
