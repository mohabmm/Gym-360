using GymApp.Common.Presentation;
using GymApp.Modules.Nutrition.Application.Diets;
using GymApp.Modules.Nutrition.Application.DTOs;
using MediatR;
using Microsoft.AspNetCore.Http;

namespace GymApp.Modules.Nutrition.Presentation.Endpoints;

/// <summary>
/// Ready Egyptian diets, part of a new member's onboarding: the coach calculates the member's
/// daily calories, picks a ready diet scaled to them and gives it; the member ticks today's meals.
/// The gym comes from the token (as the training plan endpoints).
/// </summary>
internal sealed class DietEndpoints : IEndpoint
{
    internal sealed record GiveDietRequest(string DietKey, DietTargetInput Target);

    internal sealed record CompleteMealRequest(bool Alternative = false);

    private const string Tag = "Nutrition – ready diets";

    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        MapCoach(app);
        MapMember(app);
    }

    private static void MapCoach(IEndpointRouteBuilder app)
    {
        var coach = app.MapGroup("api/v1/nutrition").RequireAuthorization("trainer").WithTags(Tag);

        coach.MapPost("calorie-target", (BodyInput body, HttpContext http, ISender sender) =>
            SendAsync(http, (gymId, _) => new CalculateCalorieTargetQuery(gymId, body), sender))
            .WithSummary("Daily calories and macros for a member (Mifflin-St Jeor, activity and goal)")
            .Produces<ApiResponse<DailyTargetDto>>(StatusCodes.Status200OK);

        coach.MapGet("ready-diets", (string? goal, HttpContext http, ISender sender) =>
            SendAsync(http, (gymId, _) => new ListReadyDietsQuery(gymId, goal), sender))
            .WithSummary("Ready Egyptian diets, the ones for the goal first")
            .Produces<ApiResponse<IReadOnlyList<ReadyDietSummaryDto>>>(StatusCodes.Status200OK);

        coach.MapGet("ready-diets/{key}", (string key, int calories, HttpContext http, ISender sender) =>
            SendAsync(http, (gymId, _) => new PreviewReadyDietQuery(gymId, key, calories), sender))
            .WithSummary("A ready diet's week, portions scaled to the calories")
            .Produces<ApiResponse<ReadyDietPreviewDto>>(StatusCodes.Status200OK);

        coach.MapPost("clients/{clientId:guid}/diet", (Guid clientId, GiveDietRequest request, HttpContext http, ISender sender) =>
            SendAsync(http, (gymId, coachId) => new GiveDietCommand(new DietCoach(gymId, coachId), clientId, request.DietKey, request.Target), sender))
            .WithSummary("Give your client a ready diet scaled to their target (they are notified)")
            .Produces<ApiResponse<MealPlanDetailDto>>(StatusCodes.Status200OK);

        coach.MapGet("clients/{clientId:guid}/diet", (Guid clientId, HttpContext http, ISender sender) =>
            SendAsync(http, (gymId, coachId) => new GetClientDietQuery(new DietCoach(gymId, coachId), clientId), sender))
            .WithSummary("Your client's current diet and how many meals they ticked each of the last 7 days")
            .Produces<ApiResponse<ClientDietDto>>(StatusCodes.Status200OK);
    }

    private static void MapMember(IEndpointRouteBuilder app)
    {
        var member = app.MapGroup("api/v1/nutrition/me/meal-plan/today").RequireAuthorization("trainee").WithTags(Tag);

        member.MapGet("", (HttpContext http, ISender sender) =>
            SendAsync(http, (gymId, memberId) => new GetTodayMealsQuery(memberId, gymId), sender))
            .WithSummary("Today's meals from my diet, each with whether I ticked it (data null = no diet yet)")
            .Produces<ApiResponse<TodayMealsDto?>>(StatusCodes.Status200OK);

        member.MapPost("{index:int}/done", (int index, CompleteMealRequest? request, HttpContext http, ISender sender) =>
            SendAsync(http, (gymId, memberId) => new CompleteTodayMealCommand(memberId, gymId, index, request?.Alternative ?? false), sender))
            .WithSummary("I ate this meal (or its alternative): logs it in today's food log, once")
            .Produces<ApiResponse<TodayMealsDto>>(StatusCodes.Status200OK);
    }

    /// <summary>Resolves the caller from the token (gym, user), sends the request <paramref name="build"/> makes, maps the result.</summary>
    private static async Task<IResult> SendAsync<T>(HttpContext http, Func<Guid, Guid, IRequest<GymApp.Common.Domain.Result<T>>> build, ISender sender)
    {
        if (!CallerContext.TryGetUserId(http, out var userId))
            return Results.Unauthorized();
        if (!CallerContext.TryGetGymId(http, out var gymId))
            return ApiResults.Problem("GYM_CONTEXT_REQUIRED", "Select a gym first.", StatusCodes.Status400BadRequest);
        var result = await sender.Send(build(gymId, userId));
        return result.IsSuccess ? ApiResults.Ok(result.Value) : ApiResults.Problem(result.Error);
    }
}
