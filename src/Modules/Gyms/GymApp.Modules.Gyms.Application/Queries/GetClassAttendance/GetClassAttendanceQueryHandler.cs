using GymApp.Common.Application.Messaging;
using GymApp.Common.Domain;
using GymApp.Modules.Gyms.Application.Contracts;
using GymApp.Modules.Gyms.Application.DTOs;
using GymApp.Modules.Gyms.Domain.Errors;
using GymApp.Modules.Gyms.Domain.ValueObjects;

namespace GymApp.Modules.Gyms.Application.Queries.GetClassAttendance;

internal sealed class GetClassAttendanceQueryHandler : IQueryHandler<GetClassAttendanceQuery, List<MemberDto>>
{
    private readonly IGymQueryService _gymQueryService;

    public GetClassAttendanceQueryHandler(IGymQueryService gymQueryService)
    {
        _gymQueryService = gymQueryService;
    }

    public async Task<Result<List<MemberDto>>> Handle(GetClassAttendanceQuery request, CancellationToken cancellationToken)
    {
        var members = await _gymQueryService.GetClassAttendanceAsync(new GymId(request.GymId), request.ClassId, cancellationToken);

        if (members is null)
            return Result.Failure<List<MemberDto>>(GymErrors.ClassNotFound);

        return Result.Success(members);
    }
}
