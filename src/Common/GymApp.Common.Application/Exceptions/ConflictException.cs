namespace GymApp.Common.Application.Exceptions;

/// <summary>
/// Thrown when a write violates a uniqueness/state invariant that a race let slip
/// past the in-memory domain guard (e.g. a filtered unique index trip at SaveChanges).
/// <see cref="GymApp.Api.Middleware.GlobalExceptionHandler"/> maps it to HTTP 409 so
/// clients get a clean conflict instead of a 500.
/// </summary>
public sealed class ConflictException : Exception
{
    public ConflictException(string code, string message) : base(message)
    {
        Code = code;
    }

    public string Code { get; }
}
