using GymApp.Common.Domain;
using Microsoft.AspNetCore.Http;

namespace GymApp.Common.Presentation;

/// <summary>
/// Maps a domain <see cref="Error"/>'s code to the HTTP status code clients expect,
/// per the contract in docs/01_GYMAPP_BACKEND_COMPLETE.md and docs/PRD_TRAINEE_ME_ENDPOINTS.md.
/// Endpoints call this (via <see cref="ApiResults.Problem(Error)"/>) instead of hard-coding status
/// codes so plan/suspension/membership denials surface consistently as 403 and validation/not-found
/// map correctly.
/// </summary>
public static class ErrorStatusCodeMapper
{
    public static int ToStatusCode(Error error)
    {
        var code = error.Code;

        if (code.StartsWith("PLAN.", StringComparison.Ordinal))
            return StatusCodes.Status403Forbidden;

        return code switch
        {
            "GYM_SUSPENDED" => StatusCodes.Status403Forbidden,
            "NOT_A_MEMBER_OF_GYM" => StatusCodes.Status403Forbidden,
            "MEMBER_ROLE_REQUIRED" => StatusCodes.Status403Forbidden,
            "GYM_CONTEXT_REQUIRED" => StatusCodes.Status400BadRequest,
            "VALIDATION_ERROR" => StatusCodes.Status400BadRequest,
            "UNAUTHORIZED" => StatusCodes.Status401Unauthorized,
            "NOT_FOUND" => StatusCodes.Status404NotFound,
            "CONFLICT" => StatusCodes.Status409Conflict,
            _ when code.EndsWith("NotFound", StringComparison.Ordinal) => StatusCodes.Status404NotFound,
            _ when code.EndsWith(".AlreadyExists", StringComparison.Ordinal) => StatusCodes.Status409Conflict,
            _ when code.StartsWith("Progress.Duplicate", StringComparison.Ordinal) => StatusCodes.Status409Conflict,
            _ when code.EndsWith(".InvalidCredentials", StringComparison.Ordinal) => StatusCodes.Status401Unauthorized,
            _ when code.EndsWith(".InvalidToken", StringComparison.Ordinal) => StatusCodes.Status401Unauthorized,
            _ when code.EndsWith(".RefreshTokenExpired", StringComparison.Ordinal) => StatusCodes.Status401Unauthorized,
            _ when code.EndsWith(".RefreshTokenRevoked", StringComparison.Ordinal) => StatusCodes.Status401Unauthorized,
            _ when code.EndsWith(".EmailNotVerified", StringComparison.Ordinal) => StatusCodes.Status401Unauthorized,
            _ when code.EndsWith(".AccountSuspended", StringComparison.Ordinal) => StatusCodes.Status403Forbidden,
            _ when code.EndsWith(".AccountDeleted", StringComparison.Ordinal) => StatusCodes.Status403Forbidden,
            _ when code.EndsWith(".Unauthorized", StringComparison.Ordinal) => StatusCodes.Status401Unauthorized,
            _ when code.EndsWith(".Forbidden", StringComparison.Ordinal) => StatusCodes.Status403Forbidden,
            _ => StatusCodes.Status400BadRequest,
        };
    }
}
