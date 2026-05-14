using GymApp.Common.Presentation;
using GymApp.Modules.Gyms.Application.Commands.SaveMemberOnboarding;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace GymApp.Modules.Gyms.Presentation.Endpoints;

internal sealed class SaveMemberOnboardingEndpoint : IEndpoint
{
    internal sealed record Request(
        string? MedicalHistory,
        string? FitnessGoals,
        string? Injuries,
        string? Medications,
        string? FitnessLevel);

    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost("api/v1/gyms/{gymId:guid}/members/{memberId:guid}/onboarding",
            async (Guid gymId, Guid memberId, Request request, ISender sender) =>
        {
            var command = new SaveMemberOnboardingCommand(
                gymId,
                memberId,
                request.MedicalHistory,
                request.FitnessGoals,
                request.Injuries,
                request.Medications,
                request.FitnessLevel);

            var result = await sender.Send(command);

            return result.IsSuccess
                ? ApiResults.Created(result.Value)
                : ApiResults.Problem(result.Error);
        })
        .WithTags("Gyms")
        .WithSummary("Save member onboarding/intake form")
        .WithDescription("Saves or updates the onboarding intake form for a gym member, including medical history, fitness goals, injuries, medications, and fitness level.")
        .Produces<ApiResponse<Guid>>(201)
        .Produces<ApiResponse<object>>(400)
        .Produces<ApiResponse<object>>(401)
        .RequireAuthorization();
    }
}
