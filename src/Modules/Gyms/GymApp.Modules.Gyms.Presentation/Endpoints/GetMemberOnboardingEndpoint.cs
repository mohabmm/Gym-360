using System.Security.Claims;
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
            async (Guid gymId, Guid memberId, HttpContext httpContext, ISender sender) =>
        {
            // The intake form is health data (PDPL): only the member themself, or a
            // trainer/staff principal scoped to this gym, or a platform admin may read it.
            if (!CanRead(httpContext.User, gymId, memberId))
                return ApiResults.Problem(
                    "FORBIDDEN", "You are not allowed to view this member's intake form.", 403);

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
        .RequireAuthorization();
    }

    internal static bool CanRead(ClaimsPrincipal user, Guid gymId, Guid memberId)
    {
        if (!string.IsNullOrEmpty(user.FindFirstValue("adminRole")))
            return true;

        var sub = user.FindFirstValue(ClaimTypes.NameIdentifier) ?? user.FindFirstValue("sub");
        if (Guid.TryParse(sub, out var userId) && userId == memberId)
            return true;

        var scopedToGym = Guid.TryParse(user.FindFirstValue("gymId"), out var tokenGymId)
            && tokenGymId == gymId;
        if (!scopedToGym)
            return false;

        var role = user.FindFirstValue(ClaimTypes.Role) ?? user.FindFirstValue("role");
        return string.Equals(role, "trainer", StringComparison.OrdinalIgnoreCase)
            || !string.IsNullOrEmpty(user.FindFirstValue("staffRole"));
    }
}
