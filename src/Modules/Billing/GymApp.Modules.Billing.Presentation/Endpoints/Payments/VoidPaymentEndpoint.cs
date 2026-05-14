using GymApp.Common.Presentation;
using GymApp.Modules.Billing.Application.Commands.Payments;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace GymApp.Modules.Billing.Presentation.Endpoints.Payments;

internal sealed class VoidPaymentEndpoint : IEndpoint
{
    internal sealed record VoidPaymentRequest(string Reason);

    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost("api/v1/billing/payments/{paymentId:guid}/void", async (
            Guid paymentId,
            VoidPaymentRequest request,
            ISender sender) =>
        {
            var command = new VoidPaymentCommand(paymentId, request.Reason);
            var result = await sender.Send(command);

            return result.IsSuccess
                ? Results.NoContent()
                : ApiResults.Problem(result.Error);
        })
        .RequireAuthorization()
        .WithSummary("Void a payment")
        .WithDescription("Marks a payment as voided with a required reason; no refund is issued per MVP policy.")
        .Produces(204)
        .WithTags("Billing")
        .Produces<ApiResponse<object>>(400)
        .Produces<ApiResponse<object>>(401)
        .Produces<ApiResponse<object>>(404);
    }
}
