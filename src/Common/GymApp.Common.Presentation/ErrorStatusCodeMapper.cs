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
    // Exact error codes from the shared API contract.
    private static readonly Dictionary<string, int> ExactCodes = new(StringComparer.Ordinal)
    {
        ["GYM_SUSPENDED"] = StatusCodes.Status403Forbidden,
        ["NOT_A_MEMBER_OF_GYM"] = StatusCodes.Status403Forbidden,
        ["MEMBER_ROLE_REQUIRED"] = StatusCodes.Status403Forbidden,
        ["GYM_CONTEXT_REQUIRED"] = StatusCodes.Status400BadRequest,
        ["VALIDATION_ERROR"] = StatusCodes.Status400BadRequest,
        ["UNAUTHORIZED"] = StatusCodes.Status401Unauthorized,
        ["NOT_FOUND"] = StatusCodes.Status404NotFound,
        ["CONFLICT"] = StatusCodes.Status409Conflict,
    };

    // Module error-code conventions, evaluated in order; the first match wins.
    private static readonly (Func<string, bool> Matches, int StatusCode)[] PatternRules =
    {
        (EndsWith("NotFound"), StatusCodes.Status404NotFound),
        (EndsWith(".AlreadyExists"), StatusCodes.Status409Conflict),
        (StartsWith("Progress.Duplicate"), StatusCodes.Status409Conflict),
        (EndsWith(".InvalidCredentials"), StatusCodes.Status401Unauthorized),
        (EndsWith(".InvalidToken"), StatusCodes.Status401Unauthorized),
        (EndsWith(".RefreshTokenExpired"), StatusCodes.Status401Unauthorized),
        (EndsWith(".RefreshTokenRevoked"), StatusCodes.Status401Unauthorized),
        (EndsWith(".EmailNotVerified"), StatusCodes.Status401Unauthorized),
        (EndsWith(".AccountSuspended"), StatusCodes.Status403Forbidden),
        (EndsWith(".AccountDeleted"), StatusCodes.Status403Forbidden),
        (EndsWith(".AlreadyActiveInAnotherGym"), StatusCodes.Status409Conflict),
        (EndsWith(".Unauthorized"), StatusCodes.Status401Unauthorized),
        (EndsWith(".Forbidden"), StatusCodes.Status403Forbidden),
    };

    public static int ToStatusCode(Error error)
    {
        var code = error.Code;

        if (code.StartsWith("PLAN.", StringComparison.Ordinal))
            return StatusCodes.Status403Forbidden;

        if (ExactCodes.TryGetValue(code, out var exact))
            return exact;

        foreach (var (matches, statusCode) in PatternRules)
        {
            if (matches(code))
                return statusCode;
        }

        return StatusCodes.Status400BadRequest;
    }

    private static Func<string, bool> EndsWith(string suffix) =>
        code => code.EndsWith(suffix, StringComparison.Ordinal);

    private static Func<string, bool> StartsWith(string prefix) =>
        code => code.StartsWith(prefix, StringComparison.Ordinal);
}
