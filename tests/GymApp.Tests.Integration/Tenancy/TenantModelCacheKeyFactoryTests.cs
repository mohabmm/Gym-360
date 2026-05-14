using GymApp.Common.Application.Tenancy;
using GymApp.Common.Infrastructure.Tenancy;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.DependencyInjection;

namespace GymApp.Tests.Integration.Tenancy;

public sealed class TenantModelCacheKeyFactoryTests
{
    private sealed class StubTenant : ITenantContext
    {
        public Guid? GymId { get; init; }
        public string SchemaPrefix { get; init; } = string.Empty;
        public bool IsResolved => GymId.HasValue && !string.IsNullOrEmpty(SchemaPrefix);
        public bool IsHardIsolation { get; init; }
        public bool IsOwnerScope { get; init; }
        public string SchemaFor(string moduleSchema) => IsHardIsolation && IsResolved
            ? $"{SchemaPrefix}_{moduleSchema}"
            : moduleSchema;
    }

    private sealed class FakeDbContext : DbContext
    {
        public FakeDbContext(DbContextOptions<FakeDbContext> options) : base(options) { }
    }

    private static FakeDbContext Build(ITenantContext tenant)
    {
        var services = new ServiceCollection();
        services.AddSingleton(tenant);
        var sp = services.BuildServiceProvider();

        // We never open the connection; we just need a valid DbContextOptions
        // so we can resolve ITenantContext from the application service provider
        // inside the factory. SqlServer is the project's real provider.
        var opts = new DbContextOptionsBuilder<FakeDbContext>()
            .UseSqlServer("Server=stub;Database=stub;Trusted_Connection=true;")
            .UseApplicationServiceProvider(sp)
            .Options;

        return new FakeDbContext(opts);
    }

    [Fact]
    public void Different_Schema_Prefixes_Produce_Different_Cache_Keys()
    {
        var ctxG1 = Build(new StubTenant { GymId = Guid.NewGuid(), SchemaPrefix = "g1", IsHardIsolation = true });
        var ctxG2 = Build(new StubTenant { GymId = Guid.NewGuid(), SchemaPrefix = "g2", IsHardIsolation = true });

        var factory = new TenantModelCacheKeyFactory();
        var keyG1 = factory.Create(ctxG1);
        var keyG2 = factory.Create(ctxG2);

        keyG1.Should().NotBe(keyG2);
    }

    [Fact]
    public void Soft_Mode_Tenants_Share_Cache_Key()
    {
        var ctxA = Build(new StubTenant { IsHardIsolation = false });
        var ctxB = Build(new StubTenant { IsHardIsolation = false });

        var factory = new TenantModelCacheKeyFactory();
        factory.Create(ctxA).Should().Be(factory.Create(ctxB));
    }

    [Fact]
    public void Different_Context_Types_Produce_Different_Keys_Even_With_Same_Tenant()
    {
        // The factory bakes context type into the key — contexts with the same prefix
        // must not share a model cache.
        var tenant = new StubTenant { GymId = Guid.NewGuid(), SchemaPrefix = "g1", IsHardIsolation = true };

        var fakeCtx = Build(tenant);
        var factory = new TenantModelCacheKeyFactory();

        var keyDesignTime = factory.Create(fakeCtx, designTime: true);
        var keyRuntime = factory.Create(fakeCtx, designTime: false);

        keyDesignTime.Should().NotBe(keyRuntime);
    }
}
