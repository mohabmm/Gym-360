using GymApp.Common.Application.Exceptions;
using GymApp.Common.Domain;
using GymApp.Common.Infrastructure.Outbox;
using GymApp.Modules.Identity.Application.Abstractions;
using GymApp.Modules.Identity.Domain.Entities;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace GymApp.Modules.Identity.Infrastructure.Persistence;

internal sealed class IdentityDbContext : DbContext, IIdentityUnitOfWork
{
    public IdentityDbContext(DbContextOptions<IdentityDbContext> options)
        : base(options)
    {
    }

    public DbSet<User> Users => Set<User>();
    public DbSet<UserConsent> UserConsents => Set<UserConsent>();
    public DbSet<OtpCode> OtpCodes => Set<OtpCode>();
    public DbSet<PlatformAdmin> PlatformAdmins => Set<PlatformAdmin>();
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();
    public DbSet<PlatformSetting> PlatformSettings => Set<PlatformSetting>();
    public DbSet<Owner> Owners => Set<Owner>();
    public DbSet<Contract> Contracts => Set<Contract>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema("identity");

        modelBuilder.ApplyConfigurationsFromAssembly(typeof(IdentityDbContext).Assembly);

        // Configure OutboxMessage for the shared outbox pattern
        modelBuilder.Entity<OutboxMessage>(entity =>
        {
            entity.ToTable("outbox_messages", "identity");
            entity.HasKey(e => e.Id);
        });
    }

    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            return await base.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException ex) when (IsActiveMembershipViolation(ex))
        {
            // One-Gym filtered unique index (UX_user_gym_memberships_active_per_user)
            // tripped by a race the in-memory LinkToGym guard couldn't pre-empt. Translate
            // the would-be 500 into a clean 409 (see GlobalExceptionHandler).
            throw new ConflictException(
                "User.AlreadyActiveInAnotherGym",
                "This person already has an active membership at another gym (One-Gym rule).");
        }
    }

    // SQL Server unique-constraint/index violation = error 2627 (constraint) or 2601 (index).
    private static bool IsActiveMembershipViolation(DbUpdateException ex) =>
        ex.InnerException is SqlException sql
        && sql.Number is 2601 or 2627
        && sql.Message.Contains("UX_user_gym_memberships_active_per_user", StringComparison.OrdinalIgnoreCase);
}
