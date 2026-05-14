using GymApp.Common.Domain;
using Microsoft.AspNetCore.Http;

namespace GymApp.Common.Presentation;

public sealed record ApiResponse<T>(T? Data, IReadOnlyList<ApiError>? Errors = null, object? Meta = null);

public sealed record ApiError(string Code, string Message, object? Meta = null);

public static class ApiResults
{
    public static IResult Ok<T>(T data, object? meta = null)
        => Results.Ok(new ApiResponse<T>(data, null, meta));

    public static IResult Created<T>(T data, string? location = null)
        => Results.Created(location ?? "", new ApiResponse<T>(data));

    public static IResult Problem(string code, string message, int statusCode = 400, object? meta = null)
        => Results.Json(
            new ApiResponse<object>(null, new[] { new ApiError(code, message, meta) }),
            statusCode: statusCode);

    /// <summary>
    /// Maps a domain <see cref="Error"/> to the standard error envelope, deriving the HTTP
    /// status code from the error's code prefix (see <see cref="ErrorStatusCodeMapper"/>).
    /// Any structured <see cref="Error.Meta"/> flows through to clients as <c>errors[0].meta</c>.
    /// </summary>
    public static IResult Problem(Error error)
        => Problem(error.Code, error.Name, ErrorStatusCodeMapper.ToStatusCode(error), error.Meta);

    public static IResult ValidationErrors(IReadOnlyList<ApiError> errors)
        => Results.Json(new ApiResponse<object>(null, errors), statusCode: 400);
}
