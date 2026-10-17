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
            BackfillExistingMember(existing, n);
        else
            await _userRepository.AddAsync(CreateImportedTrainee(n, phoneResult.Value), ct);

        await _unitOfWork.SaveChangesAsync(ct);
    }

    private void BackfillExistingMember(User existing, DataImportMemberIntegrationEvent n)
    {
        // Backfill NationalId for an already-known member so a migration import
        // still lands the identity number even when the person pre-exists.
        existing.SetNationalId(n.NationalId);

        if (!existing.GymMemberships.Any(m => m.IsActiveAt(n.GymId)))
            existing.LinkToGym(n.GymId, UserRole.Trainee, n.Section, n.MemberNumber);

        _userRepository.Update(existing);
    }

    private static User CreateImportedTrainee(DataImportMemberIntegrationEvent n, PhoneNumber phone)
    {
        var user = User.Create(
            new NewUserDetails
            {
                Email = ResolveEmail(n),
                PhoneNumber = phone,
                PasswordHash = string.Empty,
                FirstName = n.FirstName,
                LastName = n.LastName,
                Gender = ParseGenderOrDefault(n.Gender),
                NationalId = n.NationalId
            },
            UserRole.Trainee);

        user.LinkToGym(n.GymId, UserRole.Trainee, n.Section, n.MemberNumber);
        return user;
    }

    // Falls back to a synthetic, gym-scoped address when the row has no (valid) email.
    private static Email ResolveEmail(DataImportMemberIntegrationEvent n) =>
        TryCreateEmail(n.Email)
        ?? Email.Create($"imported-{n.MemberNumber}-{n.GymId:N}@gymapp.local").Value;

    private static Email? TryCreateEmail(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
            return null;

        var emailResult = Email.Create(raw);
        return emailResult.IsSuccess ? emailResult.Value : null;
    }

    private static Gender ParseGenderOrDefault(string? raw) =>
        Enum.TryParse<Gender>(raw, ignoreCase: true, out var gender) ? gender : Gender.Male;
}
