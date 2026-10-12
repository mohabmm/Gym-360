using GymApp.Common.Presentation;
using GymApp.Modules.Gyms.Application.Queries.GetMemberOnboarding;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace GymApp.Modules.Gyms.Presentation.Endpoints;

internal sealed class GetMemberOnboardingEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet("api/v1/gyms/{gymId:guid}/members/{memberId:guid}/onboarding",
            async (Guid gymId, Guid memberId, ISender sender) =>
        {
            var result = await sender.Send(new GetMemberOnboardingQuery(gymId, memberId));

            return result.IsSuccess
                ? ApiResults.Ok(result.Value)
                : ApiResults.Problem(result.Error);
        })
        .WithTags("Gyms")
        .WithSummary("Get member onboarding/intake form")
        .WithDescription("Returns the saved intake form (medical history, fitness goals, injuries, medications, fitness level) for a gym member, or null data when none has been saved yet.")
        .Produces<ApiResponse<MemberOnboardingDto>>(200)
        .Produces<ApiResponse<object>>(401)
        .Produces<ApiResponse<object>>(403)
        .RequireAuthorization()
        // Health data (PDPL): the member themself, or trainer/staff of this gym, or an admin.
        .RequireMemberAccess(MemberAccessLevel.StaffTrainerOrSelf);
    }
}
