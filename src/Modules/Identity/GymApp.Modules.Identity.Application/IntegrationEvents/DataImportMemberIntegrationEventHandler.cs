using GymApp.Modules.Gyms.IntegrationEvents;
using GymApp.Modules.Identity.Application.Abstractions;
using GymApp.Modules.Identity.Domain.Entities;
using GymApp.Modules.Identity.Domain.Enums;
using GymApp.Modules.Identity.Domain.Repositories;
using GymApp.Modules.Identity.Domain.ValueObjects;
using MediatR;
using Microsoft.Extensions.Logging;

namespace GymApp.Modules.Identity.Application.IntegrationEvents;

internal sealed class DataImportMemberIntegrationEventHandler : INotificationHandler<DataImportMemberIntegrationEvent>
{
    private readonly IUserRepository _userRepository;
    private readonly IIdentityUnitOfWork _unitOfWork;
    private readonly ILogger<DataImportMemberIntegrationEventHandler> _logger;

    public DataImportMemberIntegrationEventHandler(
        IUserRepository userRepository,
        IIdentityUnitOfWork unitOfWork,
        ILogger<DataImportMemberIntegrationEventHandler> logger)
    {
        _userRepository = userRepository;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task Handle(DataImportMemberIntegrationEvent n, CancellationToken ct)
    {
        var phoneResult = PhoneNumber.Create(n.Phone);
        if (phoneResult.IsFailure)
        {
            _logger.LogWarning("DataImport job {JobId} row {Row}: invalid phone '{Phone}'", n.ImportJobId, n.RowNumber, n.Phone);
            return;
        }

        var existing = await _userRepository.GetByPhoneAsync(phoneResult.Value, ct);
        if (existing is not null)
        {
            if (existing.GymMemberships.Any(m => m.GymId == n.GymId && m.IsActive))
                return;

            existing.LinkToGym(n.GymId, UserRole.Trainee, n.Section, n.MemberNumber);
            _userRepository.Update(existing);
            await _unitOfWork.SaveChangesAsync(ct);
            return;
        }

        Email? email = null;
        if (!string.IsNullOrWhiteSpace(n.Email))
        {
            var emailResult = Email.Create(n.Email);
            if (emailResult.IsSuccess) email = emailResult.Value;
        }

        email ??= Email.Create($"imported-{n.MemberNumber}-{n.GymId:N}@gymapp.local").Value;

        if (!Enum.TryParse<Gender>(n.Gender, ignoreCase: true, out var gender))
            gender = Gender.Male;

        var user = User.Create(
            email,
            phoneResult.Value,
            passwordHash: string.Empty,
            firstName: n.FirstName,
            lastName: n.LastName,
            gender: gender,
            role: UserRole.Trainee);

        user.LinkToGym(n.GymId, UserRole.Trainee, n.Section, n.MemberNumber);

        await _userRepository.AddAsync(user, ct);
        await _unitOfWork.SaveChangesAsync(ct);
    }
}
