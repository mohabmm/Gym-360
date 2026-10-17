using System.Net;
using System.Net.Mail;
using GymApp.Modules.Identity.Application.Contracts;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace GymApp.Modules.Identity.Infrastructure.Services;

/// <summary>
/// Sends OTP codes via SMTP email when configured, or logs them (dev/test).
/// Set Otp:Provider = "Smtp" and configure Otp:Smtp:* in appsettings for email delivery.
/// In development (Otp:Provider = "Log"), the code is written to the application log.
/// </summary>
internal sealed class OtpSender : IOtpSender
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<OtpSender> _logger;

    public OtpSender(IConfiguration configuration, ILogger<OtpSender> logger)
    {
        _configuration = configuration;
        _logger = logger;
    }

    public async Task SendAsync(
        string recipient,
        string code,
        string purpose,
        CancellationToken cancellationToken = default)
    {
        var provider = _configuration["Otp:Provider"] ?? "Log";

        if (provider.Equals("Smtp", StringComparison.OrdinalIgnoreCase))
        {
            await SendViaSmtpAsync(recipient, code, purpose, cancellationToken);
        }
        else
        {
            _logger.LogInformation(
                "[OTP-DEV] Recipient={Recipient} Purpose={Purpose} Code={Code}",
                recipient, purpose, code);
        }
    }

    private async Task SendViaSmtpAsync(
        string recipient,
        string code,
        string purpose,
        CancellationToken cancellationToken)
    {
        var settings = SmtpSettings.Load(_configuration);

        var (subject, body) = BuildEmailContent(code, purpose);

        using var client = CreateSmtpClient(settings);

        using var message = new MailMessage
        {
            From = new MailAddress(settings.From, settings.FromName),
            Subject = subject,
            Body = body,
            IsBodyHtml = true
        };
        message.To.Add(recipient);

        await client.SendMailAsync(message, cancellationToken);

        _logger.LogInformation(
            "[OTP] Email sent to {Recipient} for purpose {Purpose}",
            recipient, purpose);
    }

    private static SmtpClient CreateSmtpClient(SmtpSettings settings) => new(settings.Host, settings.Port)
    {
        EnableSsl = settings.UseSsl,
        Credentials = string.IsNullOrEmpty(settings.Username)
            ? CredentialCache.DefaultNetworkCredentials
            : new NetworkCredential(settings.Username, settings.Password)
    };

    /// <summary>Otp:Smtp:* configuration with its defaults applied.</summary>
    private sealed record SmtpSettings
    {
        public required string Host { get; init; }
        public required int Port { get; init; }
        public required bool UseSsl { get; init; }
        public required string Username { get; init; }
        public required string Password { get; init; }
        public required string From { get; init; }
        public required string FromName { get; init; }

        public static SmtpSettings Load(IConfiguration configuration) => new()
        {
            Host = configuration["Otp:Smtp:Host"]
                ?? throw new InvalidOperationException("Otp:Smtp:Host is not configured."),
            Port = int.Parse(configuration["Otp:Smtp:Port"] ?? "587"),
            UseSsl = bool.Parse(configuration["Otp:Smtp:UseSsl"] ?? "true"),
            Username = configuration["Otp:Smtp:Username"] ?? string.Empty,
            Password = configuration["Otp:Smtp:Password"] ?? string.Empty,
            From = configuration["Otp:Smtp:From"] ?? "noreply@gymapp.com",
            FromName = configuration["Otp:Smtp:FromName"] ?? "GymApp"
        };
    }

    private static (string Subject, string Body) BuildEmailContent(string code, string purpose)
    {
        var purposeLabel = purpose switch
        {
            "registration" => "Registration Verification",
            "password_reset" => "Password Reset",
            "login" => "Login Verification",
            _ => "Verification"
        };

        var subject = $"GymApp — {purposeLabel} Code: {code}";
        var body = $"""
            <div dir="ltr" style="font-family:sans-serif;max-width:480px;margin:0 auto">
              <h2 style="color:#1a1a2e">GymApp {purposeLabel}</h2>
              <p>Your verification code is:</p>
              <div style="font-size:32px;font-weight:bold;letter-spacing:8px;color:#c41e3a;
                          padding:16px;background:#f5f5f5;text-align:center;border-radius:8px">
                {code}
              </div>
              <p style="color:#666;font-size:13px">This code expires in 10 minutes. Do not share it.</p>
              <hr/>
              <p style="color:#999;font-size:11px">GymApp — Fitness Management Platform</p>
            </div>
            """;

        return (subject, body);
    }
}
