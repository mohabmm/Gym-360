using GymApp.Common.Application.FeatureFlags;
using GymApp.Common.Presentation;
using GymApp.Common.Presentation.FeatureFlags;
using GymApp.Modules.Training.Application.DTOs;
using GymApp.Modules.Training.Application.Queries.GetExercises;
using GymApp.Modules.Training.Application.TrainingPlans;
using GymApp.Modules.Training.Application.TrainingPlans.Plans;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using static GymApp.Modules.Training.Presentation.Endpoints.TrainingPlans.PlanEndpointSupport;

namespace GymApp.Modules.Training.Presentation.Endpoints.TrainingPlans;

/// <summary>pt.training_plan — a coach assigns and edits a trainee's plan.</summary>
internal sealed class TrainingPlanEndpoints : IEndpoint
{
    internal sealed record AssignRequest(Guid TraineeId, DateOnly? StartDate, Guid? TemplateId, PlanContentInput? Content);

    internal sealed record UpdateRequest(int? ExpectedVersion, PlanContentInput Content);

    internal sealed record SaveAsTemplateRequest(bool AsGymTemplate, string? Name, string? NameAr);

    internal sealed record ExerciseSearch(string? Search, int? Page, int? PageSize);

    private const int MaxPageSize = 50;

    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        var coach = app.MapGroup("api/v1/training")
            .RequireAuthorization("trainer")
            .RequireFeature(FeatureKeys.PtTrainingPlan)
            .WithTags("Training plans");

        coach.MapPost("plans", async (AssignRequest request, HttpContext http, ISender sender) =>
            TryGetActor(http, out var actor, out var problem)
                ? ToResult(await sender.Send(new AssignTrainingPlanCommand(
                    actor, request.TraineeId, request.StartDate ?? DateOnly.FromDateTime(DateTime.UtcNow), request.TemplateId, request.Content)))
                : problem!)
        .WithSummary("Give a trainee a plan (from a template or built from scratch)")
        .WithDescription("The trainee must have an active package with the coach (free sessions included). Replaces their active plan; the old one stays in history. Start date defaults to today.")
        .Produces<ApiResponse<TrainingPlanDto>>(StatusCodes.Status200OK);

        coach.MapPut("plans/{planId:guid}", async (Guid planId, UpdateRequest request, HttpContext http, ISender sender) =>
            TryGetActor(http, out var actor, out var problem)
                ? ToResult(await sender.Send(new UpdateTrainingPlanCommand(actor, planId, request.ExpectedVersion, request.Content)))
                : problem!)
        .WithSummary("Edit a trainee's active plan (the trainee is notified)")
        .WithDescription("Send the version you edited as expectedVersion; 409 Training.TrainingPlan.Conflict when the plan changed since.")
        .Produces<ApiResponse<TrainingPlanDto>>(StatusCodes.Status200OK);

        coach.MapPost("plans/{planId:guid}/save-as-template", async (Guid planId, SaveAsTemplateRequest request, HttpContext http, ISender sender) =>
            TryGetActor(http, out var actor, out var problem)
                ? ToResult(await sender.Send(new SavePlanAsTemplateCommand(actor, planId, request.AsGymTemplate, request.Name, request.NameAr)))
                : problem!)
        .WithSummary("Save a trainee's plan as a new template")
        .Produces<ApiResponse<PlanTemplateDto>>(StatusCodes.Status200OK);

        coach.MapGet("trainees/{traineeId:guid}/plans", async (Guid traineeId, HttpContext http, ISender sender) =>
            TryGetActor(http, out var actor, out var problem)
                ? ToResult(await sender.Send(new GetTraineePlansQuery(actor, traineeId)))
                : problem!)
        .WithSummary("A trainee's active plan and plan history")
        .Produces<ApiResponse<TraineePlansDto>>(StatusCodes.Status200OK);

        // The staff web's gym-template editor searches the library too.
        app.MapGet("api/v1/training/plan-exercises", async ([AsParameters] ExerciseSearch query, HttpContext http, ISender sender) =>
        {
            if (!TryGetActor(http, out var actor, out var problem))
                return problem!;
            var size = Math.Clamp(query.PageSize ?? 20, 1, MaxPageSize);
            return ToResult(await sender.Send(new GetExercisesQuery(actor.GymId, null, null, query.Search, Math.Max(query.Page ?? 1, 1), size)));
        })
        .RequireAuthorization(CoachOrGymManager)
        .RequireFeature(FeatureKeys.PtTrainingPlan)
        .WithTags("Training plans")
        .WithSummary("Search the exercise library (English or Arabic) for the plan builder")
        .Produces<ApiResponse<PagedResponse<ExerciseDto>>>(StatusCodes.Status200OK);

        // The trainee reads their own plan; the gym's owner/manager may look too.
        app.MapGet("api/v1/training/plans/{planId:guid}", async (Guid planId, HttpContext http, ISender sender) =>
            TryGetActor(http, out var actor, out var problem)
                ? ToResult(await sender.Send(new GetTrainingPlanQuery(actor, planId)))
                : problem!)
        .RequireAuthorization()
        .RequireFeature(FeatureKeys.PtTrainingPlan)
        .WithTags("Training plans")
        .WithSummary("A plan with its workout days (its trainee, coach, or the gym's owner/manager)")
        .Produces<ApiResponse<TrainingPlanDto>>(StatusCodes.Status200OK);
    }
}
