using GymApp.Common.Application.Tenancy;
using Microsoft.Extensions.Options;

namespace GymApp.Common.Infrastructure.Tenancy;

public sealed class TenantContextAccessor : ITenantContextAccessor
{
    private readonly TenancyOptions _options;

    public TenantContextAccessor(IOptions<TenancyOptions> options)
    {
        _options = options.Value;
    }

    public Guid? GymId { get; private set; }

    public string SchemaPrefix { get; private set; } = string.Empty;

    public bool IsResolved => GymId.HasValue && !string.IsNullOrEmpty(SchemaPrefix);

    public bool IsHardIsolation => _options.Mode == TenancyMode.Hard;

    public bool IsOwnerScope { get; private set; }

    public string SchemaFor(string moduleSchema)
    {
        if (string.IsNullOrEmpty(moduleSchema))
            throw new ArgumentException("Module schema name is required.", nameof(moduleSchema));

        if (!IsHardIsolation || !IsResolved)
            return moduleSchema;

        return $"{SchemaPrefix}_{moduleSchema}";
    }

    public void Set(Guid? gymId, string schemaPrefix, bool isOwnerScope)
    {
        GymId = gymId;
        SchemaPrefix = schemaPrefix ?? string.Empty;
        IsOwnerScope = isOwnerScope;
    }

    public void Reset()
    {
        GymId = null;
        SchemaPrefix = string.Empty;
        IsOwnerScope = false;
    }
}
