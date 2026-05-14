namespace GymApp.Common.Domain;

public record Error(string Code, string Name, object? Meta = null)
{
    public static readonly Error None = new(string.Empty, string.Empty);
    public static readonly Error NullValue = new("Error.NullValue", "Null value was provided");
}
