using GymApp.Common.Domain;

namespace GymApp.Common.Application.Exceptions;

public sealed class ValidationException : Exception
{
    public ValidationException(IReadOnlyList<Error> errors)
        : base("One or more validation errors occurred")
    {
        Errors = errors;
    }

    public IReadOnlyList<Error> Errors { get; }
}
