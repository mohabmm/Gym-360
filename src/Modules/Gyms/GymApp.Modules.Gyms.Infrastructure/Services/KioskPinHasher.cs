using GymApp.Modules.Gyms.Application.Abstractions;

namespace GymApp.Modules.Gyms.Infrastructure.Services;

internal sealed class KioskPinHasher : IKioskPinHasher
{
    public string Hash(string pin)
    {
        return BCrypt.Net.BCrypt.HashPassword(pin, workFactor: 12);
    }

    public bool Verify(string pin, string hash)
    {
        return BCrypt.Net.BCrypt.Verify(pin, hash);
    }
}
