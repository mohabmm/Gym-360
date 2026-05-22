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
        var result = await _gymQueryService.GetMembersAsync(
            request.GymId,
            request.Page,
            request.PageSize,
            request.Status,
            request.Section,
            cancellationToken);

        // Trainer-scoped filter: when caller supplies a TrainerId, narrow the
        // page to members on that trainer's Active session packages. Avoids
        // exposing every gym member as a "client" of every trainer (which is
        // what trainer mobile saw before this filter existed).
        if (request.TrainerId.HasValue)
        {
            var clientIds = await _trainerAssignmentLookup.GetClientsForTrainerAsync(
                request.TrainerId.Value, request.GymId, cancellationToken);
            var allowed = clientIds.ToHashSet();
            var filtered = result.Items.Where(m => allowed.Contains(m.MemberId)).ToList();
            return Result.Success(new PagedResponse<MemberDto>(
                filtered,
                Page: request.Page,
                PageSize: request.PageSize,
                TotalCount: filtered.Count,
                TotalPages: filtered.Count == 0
                    ? 0
                    : (int)Math.Ceiling(filtered.Count / (double)request.PageSize)));
        }

        return Result.Success(result);
    }
}
