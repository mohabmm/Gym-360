using System.Text;
using GymApp.Common.Application.Messaging;
using GymApp.Common.Application.Tenancy;
using GymApp.Common.Domain;
using GymApp.Modules.Gyms.Application.Abstractions;
using GymApp.Modules.Gyms.Domain.Entities;
using GymApp.Modules.Gyms.Domain.Repositories;
using GymApp.Modules.Gyms.Domain.ValueObjects;
using GymApp.Modules.Gyms.IntegrationEvents;
using MediatR;
using Microsoft.Extensions.DependencyInjection;

namespace GymApp.Modules.Gyms.Application.Commands.RegisterGym;

internal sealed class RegisterGymCommandHandler : ICommandHandler<RegisterGymCommand, Guid>
{
    private readonly IGymRepository _gymRepository;
    private readonly IGymsUnitOfWork _unitOfWork;
    private readonly IGymProvisioner _provisioner;
    private readonly ITenantContext _tenantContext;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IPublisher _publisher;

    public RegisterGymCommandHandler(
        IGymRepository gymRepository,
        IGymsUnitOfWork unitOfWork,
        IGymProvisioner provisioner,
        ITenantContext tenantContext,
        IServiceScopeFactory scopeFactory,
        IPublisher publisher)
    {
        _gymRepository = gymRepository;
        _unitOfWork = unitOfWork;
        _provisioner = provisioner;
        _tenantContext = tenantContext;
        _scopeFactory = scopeFactory;
        _publisher = publisher;
    }

    public async Task<Result<Guid>> Handle(RegisterGymCommand request, CancellationToken cancellationToken)
    {
        var address = new Address(
            request.Address.Street,
            request.Address.City,
            request.Address.State,
            request.Address.Country,
            request.Address.ZipCode,
            request.Address.Latitude,
            request.Address.Longitude);

        var contact = new ContactInfo(
            request.Contact.Email,
            request.Contact.Phone,
            request.Contact.Website);

        var gymCode = GymCode.Generate();

        var gym = Gym.Create(
            request.OwnerId,
            request.Name,
            request.Description,
            address,
            contact,
            gymCode);

        // Endpoint is platform_admin-only; admin-created gyms are pre-approved so
        // staff/member flows work immediately without a separate approve call.
        // Pass AutoApprove=false to leave the gym Pending — used by self-serve owner
        // signup flows where admin reviews + approves/rejects.
        if (request.AutoApprove)
            gym.Approve();

        // Soft mode: single shared schema, write directly via the request-scoped repository.
        if (!_tenantContext.IsHardIsolation)
        {
            await _gymRepository.AddAsync(gym, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
            if (request.AutoApprove)
                await _publisher.Publish(new GymApprovedIntegrationEvent(gym.Id), cancellationToken);
            return Result.Success(gym.Id);
        }

        // Hard mode: provision a fresh per-gym tenant first (registry row + g{N}_*
        // schemas + tables), then persist the aggregate inside a new scope pinned
        // to that prefix so EF resolves the correct g{N}_gyms.gyms table.
        var slug = BuildSlug(request.Name, gymCode.Value);
        var registration = await _provisioner.ProvisionAsync(
            gym.Id, request.OwnerId, request.Name, slug, gymCode.Value, cancellationToken);

        using var scope = _scopeFactory.CreateScope();
        var sp = scope.ServiceProvider;

        var accessor = sp.GetRequiredService<ITenantContextAccessor>();
        accessor.Set(registration.GymId, registration.SchemaPrefix, isOwnerScope: false);

        var scopedRepo = sp.GetRequiredService<IGymRepository>();
        var scopedUow = sp.GetRequiredService<IGymsUnitOfWork>();

        await scopedRepo.AddAsync(gym, cancellationToken);
        await scopedUow.SaveChangesAsync(cancellationToken);

        // Cross-module fan-out: Billing handler subscribes the gym to a default plan
        // so plan-gated features (custom_exercise_library, etc.) work post-register.
        // Only fired when the gym is auto-approved; pending gyms must be approved
        // separately via /admin/gyms/{gymId}/approve which raises the same event.
        if (request.AutoApprove)
            await _publisher.Publish(new GymApprovedIntegrationEvent(gym.Id), cancellationToken);

        return Result.Success(gym.Id);
    }

    private static string BuildSlug(string name, string gymCode)
    {
        var sb = new StringBuilder();
        foreach (var ch in (name ?? string.Empty).ToLowerInvariant())
        {
            if (char.IsLetterOrDigit(ch)) sb.Append(ch);
            else if (sb.Length > 0 && sb[sb.Length - 1] != '-') sb.Append('-');
        }
        var slug = sb.ToString().Trim('-');
        if (slug.Length == 0) slug = "gym";
        if (slug.Length > 80) slug = slug.Substring(0, 80);
        return $"{slug}-{gymCode.ToLowerInvariant()}";
    }
}
