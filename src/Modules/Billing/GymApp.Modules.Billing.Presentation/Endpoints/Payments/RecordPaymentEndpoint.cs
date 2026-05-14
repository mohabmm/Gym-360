using GymApp.Common.Presentation;
using GymApp.Modules.Billing.Application.Commands.Payments;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace GymApp.Modules.Billing.Presentation.Endpoints.Payments;

internal sealed class RecordPaymentEndpoint : IEndpoint
{
    internal sealed record RecordPaymentRequest(
        Guid GymId,
        Guid MemberId,
        decimal Amount,
        string PaymentMethod,
        string Purpose,
        Guid? ReferenceId,
        Guid? TrainerId,
        int? SessionCount);

    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost("api/v1/billing/payments", async (
            RecordPaymentRequest request,
            HttpContext httpContext,
            ISender sender) =>
        {
            if (!UserContext.TryGetUserId(httpContext, out var recordedBy))
                return ApiResults.Problem(new GymApp.Common.Domain.Error("Auth.Unauthorized", "Authenticated user required."));

            var command = new RecordPaymentCommand(
                request.GymId,
                request.MemberId,
                request.Amount,
                request.PaymentMethod,
                request.Purpose,
                request.ReferenceId,
                recordedBy,
                request.TrainerId,
                request.SessionCount);

            var result = await sender.Send(command);

            return result.IsSuccess
                ? ApiResults.Created(result.Value, $"api/v1/billing/payments/{result.Value}")
                : ApiResults.Problem(result.Error);
        })
        .RequireAuthorization()
        .WithSummary("Record a payment")
        .WithDescription("Records a payment for a member. When `purpose=SessionPackage` and `referenceId` is null, supplying `trainerId` + `sessionCount` (multiple of 4) atomically creates the SessionPackage and links the new payment to it — the bidirectional 'Record Payment + Assign Trainer' flow.")
        .WithTags("Billing")
        .Produces<ApiResponse<Guid>>(201)
        .Produces<ApiResponse<object>>(400)
        .Produces<ApiResponse<object>>(401);
    }
}
