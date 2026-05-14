using GymApp.Common.Application.Tenancy;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;

namespace GymApp.Common.Infrastructure.Tenancy;

public sealed class TenantModelCacheKeyFactory : IModelCacheKeyFactory
{
    public object Create(DbContext context, bool designTime)
    {
        var tenant = context.GetService<ITenantContext>();
        var key = tenant.IsHardIsolation && tenant.IsResolved
            ? tenant.SchemaPrefix
            : "__soft__";

        return (context.GetType(), key, designTime);
    }

    public object Create(DbContext context) => Create(context, designTime: false);
}
