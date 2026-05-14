namespace GymApp.Common.Application.Tenancy;

public interface ITenantContext
{
    Guid? GymId { get; }

    string SchemaPrefix { get; }

    bool IsResolved { get; }

    bool IsHardIsolation { get; }

    bool IsOwnerScope { get; }

    string SchemaFor(string moduleSchema);
}
