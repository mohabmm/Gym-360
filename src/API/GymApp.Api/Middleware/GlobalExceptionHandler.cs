using System.Diagnostics;
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
        var (statusCode, errors) = MapException(exception);

        // Log level by severity: 5xx faults are errors, 4xx are expected client errors.
        if (statusCode >= StatusCodes.Status500InternalServerError)
            _logger.LogError(exception, "Unhandled exception occurred: {Message}", exception.Message);
        else
            _logger.LogWarning("Request rejected on {Path} ({StatusCode}): {Message}",
                httpContext.Request.Path, statusCode, exception.Message);

        RecordOnActiveSpan(exception, statusCode);

        httpContext.Response.StatusCode = statusCode;

        await httpContext.Response.WriteAsJsonAsync(
            new ApiResponse<object>(null, errors),
            cancellationToken);

        return true;
    }

    // ValidationBehavior in the MediatR pipeline converts FluentValidation failures into
    // our typed ValidationException with code="VALIDATION_ERROR" and field in Meta.
    private static (int StatusCode, List<ApiError> Errors) MapException(Exception exception) =>
        exception is ValidationException validationException
            ? (StatusCodes.Status400BadRequest,
               validationException.Errors
                   .Select(e => new ApiError(e.Code, e.Name, e.Meta))
                   .ToList())
            : MapSingleErrorException(exception);

    private static (int StatusCode, List<ApiError> Errors) MapSingleErrorException(Exception exception) => exception switch
    {
        BadHttpRequestException badRequest => Single(StatusCodes.Status400BadRequest, "BAD_REQUEST", badRequest.Message),

        // Uniqueness/state invariant tripped at the DB after a race slipped past the
        // in-memory domain guard (translated in the module DbContext). Surface as 409.
        ConflictException conflict => Single(StatusCodes.Status409Conflict, conflict.Code, conflict.Message),

        UnauthorizedAccessException => Single(StatusCodes.Status401Unauthorized, "UNAUTHORIZED", "Authentication is required."),

        KeyNotFoundException => Single(StatusCodes.Status404NotFound, "NOT_FOUND", exception.Message),

        _ => Single(StatusCodes.Status500InternalServerError, "INTERNAL_ERROR", "An unexpected error occurred.")
    };

    private static (int StatusCode, List<ApiError> Errors) Single(int statusCode, string code, string message) =>
        (statusCode, new List<ApiError> { new(code, message) });

    // Record EVERY exception on the active span so all of them — validation, bad request,
    // conflict, unauthorized, not-found, and genuine faults — surface in the SigNoz
    // Exceptions tab. The IExceptionHandler swallows the exception (returns true), so it
    // never reaches the ASP.NET Core hosting layer where OTel's RecordException would
    // fire; we record it here ourselves. The span status is set to Error only for true
    // 5xx faults — 4xx are recorded as exception events but the span stays Unset so they
    // don't inflate the service error-rate charts.
    private static void RecordOnActiveSpan(Exception exception, int statusCode)
    {
        var activity = Activity.Current;
        if (activity is null)
            return;

        activity.AddException(exception);
        if (statusCode >= StatusCodes.Status500InternalServerError)
            activity.SetStatus(ActivityStatusCode.Error, exception.Message);
    }
}
