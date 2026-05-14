namespace GymApp.Modules.Gyms.Application.Abstractions;

public interface IKioskPinHasher
{
    string Hash(string pin);
    bool Verify(string pin, string hash);
}
