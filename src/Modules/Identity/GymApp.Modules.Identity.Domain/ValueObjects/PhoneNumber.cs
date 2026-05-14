using System.Text.RegularExpressions;
using GymApp.Common.Domain;

namespace GymApp.Modules.Identity.Domain.ValueObjects;

public sealed partial class PhoneNumber : IEquatable<PhoneNumber>
{
    private PhoneNumber(string value) => Value = value;

    public string Value { get; }

    public static Result<PhoneNumber> Create(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return Result.Failure<PhoneNumber>(Errors.UserErrors.InvalidPhoneNumber);

        var cleaned = value.Trim();

        if (!PhoneRegex().IsMatch(cleaned))
            return Result.Failure<PhoneNumber>(Errors.UserErrors.InvalidPhoneNumber);

        return Result.Success(new PhoneNumber(cleaned));
    }

    [GeneratedRegex(@"^\+?[1-9]\d{7,14}$", RegexOptions.Compiled)]
    private static partial Regex PhoneRegex();

    public bool Equals(PhoneNumber? other) => other is not null && Value == other.Value;
    public override bool Equals(object? obj) => obj is PhoneNumber other && Equals(other);
    public override int GetHashCode() => Value.GetHashCode();
    public override string ToString() => Value;
}
