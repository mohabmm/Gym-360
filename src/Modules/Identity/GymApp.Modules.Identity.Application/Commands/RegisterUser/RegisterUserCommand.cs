using GymApp.Common.Application.Messaging;
using GymApp.Modules.Identity.Application.DTOs;

namespace GymApp.Modules.Identity.Application.Commands.RegisterUser;

public sealed record RegisterUserCommand(
    string Email,
    string Password,
    string FirstName,
    string LastName,
    string Phone,
    string Role,
    bool DataProcessingConsent = false,
    bool MarketingConsent = false) : ICommand<TokenResponse>;
