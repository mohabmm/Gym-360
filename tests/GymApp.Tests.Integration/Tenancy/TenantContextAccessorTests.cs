using GymApp.Common.Infrastructure.Tenancy;
using Microsoft.Extensions.Options;

namespace GymApp.Tests.Integration.Tenancy;

public sealed class TenantContextAccessorTests
{
    private static TenantContextAccessor Build(TenancyMode mode)
        => new(Options.Create(new TenancyOptions { Mode = mode }));

    [Fact]
    public void SchemaFor_SoftMode_Returns_ModuleSchema_Unchanged()
    {
        var accessor = Build(TenancyMode.Soft);
        accessor.Set(Guid.NewGuid(), "g1", isOwnerScope: false);

        accessor.SchemaFor("training").Should().Be("training");
        accessor.SchemaFor("billing").Should().Be("billing");
        accessor.IsHardIsolation.Should().BeFalse();
    }

    [Fact]
    public void SchemaFor_HardMode_WithResolvedTenant_Prefixes_Module()
    {
        var accessor = Build(TenancyMode.Hard);
        accessor.Set(Guid.NewGuid(), "g42", isOwnerScope: false);

        accessor.SchemaFor("training").Should().Be("g42_training");
        accessor.SchemaFor("billing").Should().Be("g42_billing");
        accessor.IsHardIsolation.Should().BeTrue();
        accessor.IsResolved.Should().BeTrue();
    }

    [Fact]
    public void SchemaFor_HardMode_Unresolved_Falls_Back_To_Plain_Module_Name()
    {
        // Owner who has not yet selected a gym: middleware leaves accessor unresolved.
        // Per-gym DbContexts must not error — they fall back to the plain module name
        // which only protects them from accidental usage; queries still need a resolved
        // tenant to hit real per-gym data.
        var accessor = Build(TenancyMode.Hard);

        accessor.SchemaFor("training").Should().Be("training");
        accessor.IsResolved.Should().BeFalse();
        accessor.IsHardIsolation.Should().BeTrue();
    }

    [Fact]
    public void Reset_Clears_GymId_And_SchemaPrefix()
    {
        var accessor = Build(TenancyMode.Hard);
        accessor.Set(Guid.NewGuid(), "g1", isOwnerScope: true);

        accessor.Reset();

        accessor.GymId.Should().BeNull();
        accessor.SchemaPrefix.Should().Be(string.Empty);
        accessor.IsOwnerScope.Should().BeFalse();
        accessor.IsResolved.Should().BeFalse();
    }

    [Fact]
    public void SchemaFor_EmptyModule_Throws()
    {
        var accessor = Build(TenancyMode.Hard);

        var act = () => accessor.SchemaFor("");
        act.Should().Throw<ArgumentException>();
    }
}
