namespace GymApp.Common.Application.Tenancy;

public interface ITenantContextAccessor : ITenantContext
{
    void Set(Guid? gymId, string schemaPrefix, bool isOwnerScope);

    void Reset();
}
