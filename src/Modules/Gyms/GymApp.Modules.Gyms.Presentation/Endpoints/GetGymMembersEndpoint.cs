using GymApp.Common.Presentation;
using GymApp.Modules.Gyms.Application.DTOs;
using GymApp.Modules.Gyms.Application.Queries.GetGymMembers;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace GymApp.Modules.Gyms.Presentation.Endpoints;

internal sealed class GetGymMembersEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet("api/v1/gyms/{gymId:guid}/members", async (
            Guid gymId,
            int page,
            int pageSize,
            string? status,
            string? section,
            Guid? trainerId,
            string? search,
            ISender sender) =>
        {
            // Clamp paging to safe range. pageSize=0/-N caused totalPages=int.MaxValue / 0 nonsense.
            page = page <= 0 ? 1 : page;
            pageSize = pageSize switch { <= 0 => 25, > 200 => 200, _ => pageSize };
            var query = new GetGymMembersQuery(gymId, page, pageSize, status, section, trainerId, search);

            var result = await sender.Send(query);

            return result.IsSuccess
                ? ApiResults.Ok(result.Value)
                : ApiResults.Problem(result.Error);
        })
        .WithTags("Gyms")
        .WithSummary("Get paginated gym members")
        .WithDescription("Returns a paginated list of gym members. Supports server-side filtering by membership status and gym section, and a free-text `search` over member name, email, phone and member number (so the staff roster can find members beyond the first page). Pass `trainerId` to narrow to members with an Active session package booked against that trainer (used by trainer mobile so trainers only see their own clients, not the entire gym roster).")
        .Produces<ApiResponse<PagedResponse<MemberDto>>>(200)
        .Produces<ApiResponse<object>>(401)
        .Produces<ApiResponse<object>>(404)
        .RequireAuthorization();
    }
}
