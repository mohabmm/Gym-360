namespace GymApp.Modules.Gyms.Domain.ValueObjects;

public sealed record GymCode
{
    public string Value { get; }

    private GymCode(string value) => Value = value;

    public static GymCode Create(string value)
    {
        if (string.IsNullOrWhiteSpace(value) || value.Length != 6)
            throw new ArgumentException("Gym code must be exactly 6 characters.", nameof(value));

        return new GymCode(value.ToUpperInvariant());
    }

    public static GymCode Generate()
    {
        const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
        var random = new Random();
        var code = new char[6];
        for (int i = 0; i < 6; i++)
            code[i] = chars[random.Next(chars.Length)];

        return new GymCode(new string(code));
    }
}
