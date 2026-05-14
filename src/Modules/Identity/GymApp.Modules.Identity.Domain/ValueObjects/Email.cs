using System.Text.RegularExpressions;
using GymApp.Common.Domain;

namespace GymApp.Modules.Identity.Domain.ValueObjects;

public sealed partial class Email : IEquatable<Email>
{
    private Email(string value) => Value = value;

    public string Value { get; }

    public static Result<Email> Create(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return Result.Failure<Email>(Errors.UserErrors.InvalidEmail);

        if (!EmailRegex().IsMatch(value))
            return Result.Failure<Email>(Errors.UserErrors.InvalidEmail);

        return Result.Success(new Email(value.ToLowerInvariant().Trim()));
    }

    [GeneratedRegex(@"^[^@\s]+@[^@\s]+\.[^@\s]+$", RegexOptions.Compiled)]
    private static partial Regex EmailRegex();

    public bool Equals(Email? other) => other is not null && Value == other.Value;
    public override bool Equals(object? obj) => obj is Email other && Equals(other);
    public override int GetHashCode() => Value.GetHashCode();
    public override string ToString() => Value;
}
