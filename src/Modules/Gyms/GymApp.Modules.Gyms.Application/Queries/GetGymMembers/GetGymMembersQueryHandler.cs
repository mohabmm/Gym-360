using GymApp.Modules.Billing.Shared;
using GymApp.Common.Application.Messaging;
using GymApp.Common.Domain;
using GymApp.Modules.Gyms.Application.Contracts;
using GymApp.Modules.Gyms.Application.DTOs;

namespace GymApp.Modules.Gyms.Application.Queries.GetGymMembers;

internal sealed class GetGymMembersQueryHandler : IQueryHandler<GetGymMembersQuery, PagedResponse<MemberDto>>
{
    private readonly IGymQueryService _gymQueryService;
    private readonly ITrainerAssignmentLookup _trainerAssignmentLookup;

    public GetGymMembersQueryHandler(
        IGymQueryService gymQueryService,
        ITrainerAssignmentLookup trainerAssignmentLookup)
    {
        _gymQueryService = gymQueryService;
        _trainerAssignmentLookup = trainerAssignmentLookup;
    }

    public async Task<Result<PagedResponse<MemberDto>>> Handle(GetGymMembersQuery request, CancellationToken cancellationToken)
    {
        // Trainer-scoped filter: when caller supplies a TrainerId, narrow the
        // roster to members on that trainer's Active session packages. Avoids
        // exposing every gym member as a "client" of every trainer (which is
        // what trainer mobile saw before this filter existed). The restriction
        // is applied inside the query BEFORE paging — filtering the fetched page
        // afterwards dropped clients that fell on later pages and reported a
        // TotalCount no larger than the page size.
        IReadOnlyCollection<Guid>? restrictToMemberIds = null;
        if (request.TrainerId.HasValue)
        {
            restrictToMemberIds = await _trainerAssignmentLookup.GetClientsForTrainerAsync(
                request.TrainerId.Value, request.GymId, cancellationToken);
        }

        var result = await _gymQueryService.GetMembersAsync(
            request.GymId,
            new MemberRosterRequest(
                request.Page, request.PageSize, request.Status, request.Section, request.Search, restrictToMemberIds),
            cancellationToken);

        return Result.Success(result);
    }
}
