namespace GymApp.Modules.Identity.Application.Contracts;

public interface IOtpSender
{
    Task SendAsync(string recipient, string code, string purpose, CancellationToken cancellationToken = default);
}
