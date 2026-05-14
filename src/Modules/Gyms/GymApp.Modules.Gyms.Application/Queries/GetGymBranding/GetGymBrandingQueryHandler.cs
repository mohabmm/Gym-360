using GymApp.Common.Application.Messaging;
using GymApp.Common.Domain;
using GymApp.Modules.Gyms.Application.Contracts;
using GymApp.Modules.Gyms.Application.DTOs;
using GymApp.Modules.Gyms.Domain.Errors;

namespace GymApp.Modules.Gyms.Application.Queries.GetGymBranding;

internal sealed class GetGymBrandingQueryHandler : IQueryHandler<GetGymBrandingQuery, BrandingResponse>
{
    private readonly IGymQueryService _gymQueryService;

    public GetGymBrandingQueryHandler(IGymQueryService gymQueryService)
    {
        _gymQueryService = gymQueryService;
    }

    public async Task<Result<BrandingResponse>> Handle(GetGymBrandingQuery request, CancellationToken cancellationToken)
    {
        var branding = await _gymQueryService.GetBrandingAsync(request.GymId, cancellationToken);

        if (branding is null)
            return Result.Failure<BrandingResponse>(GymErrors.NotFound);

        return Result.Success(branding);
    }
}
