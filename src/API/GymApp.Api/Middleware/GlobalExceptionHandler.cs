using GymApp.Common.Application.Exceptions;
using GymApp.Common.Presentation;
using Microsoft.AspNetCore.Diagnostics;

namespace GymApp.Api.Middleware;

internal sealed class GlobalExceptionHandler : IExceptionHandler
{
    private readonly ILogger<GlobalExceptionHandler> _logger;

    public GlobalExceptionHandler(ILogger<GlobalExceptionHandler> logger)
    {
        _logger = logger;
    }

    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        // Validation failures are expected client errors, not server faults — log as Warning.
        if (exception is ValidationException)
        {
            _logger.LogWarning("Validation failed on {Path}: {Message}",
                httpContext.Request.Path, exception.Message);
        }
        else
        {
            _logger.LogError(exception, "Unhandled exception occurred: {Message}", exception.Message);
        }

        var (statusCode, errors) = exception switch
        {
            // ValidationBehavior in the MediatR pipeline converts FluentValidation failures into
            // our typed ValidationException with code="VALIDATION_ERROR" and field in Meta.
            ValidationException validationException => (
                StatusCodes.Status400BadRequest,
                validationException.Errors
                    .Select(e => new ApiError(e.Code, e.Name, e.Meta))
                    .ToList()),

            BadHttpRequestException badRequest => (
                StatusCodes.Status400BadRequest,
                new List<ApiError> { new("BAD_REQUEST", badRequest.Message) }),

            // Uniqueness/state invariant tripped at the DB after a race slipped past the
            // in-memory domain guard (translated in the module DbContext). Surface as 409.
            ConflictException conflict => (
                StatusCodes.Status409Conflict,
                new List<ApiError> { new(conflict.Code, conflict.Message) }),

            UnauthorizedAccessException => (
                StatusCodes.Status401Unauthorized,
                new List<ApiError> { new("UNAUTHORIZED", "Authentication is required.") }),

            KeyNotFoundException => (
                StatusCodes.Status404NotFound,
                new List<ApiError> { new("NOT_FOUND", exception.Message) }),

            _ => (
                StatusCodes.Status500InternalServerError,
                new List<ApiError> { new("INTERNAL_ERROR", "An unexpected error occurred.") })
        };

        httpContext.Response.StatusCode = statusCode;

        await httpContext.Response.WriteAsJsonAsync(
            new ApiResponse<object>(null, errors),
            cancellationToken);

        return true;
    }
}
