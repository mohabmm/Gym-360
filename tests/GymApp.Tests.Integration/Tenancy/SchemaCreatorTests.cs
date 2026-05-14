using GymApp.Common.Infrastructure.Tenancy.Provisioning;

namespace GymApp.Tests.Integration.Tenancy;

public sealed class SchemaCreatorTests
{
    [Theory]
    [InlineData("g1")]
    [InlineData("g2")]
    [InlineData("g42")]
    [InlineData("g999")]
    [InlineData("g1234567890")]
    public void ValidatePrefix_Accepts_NumericGPrefix(string prefix)
    {
        var act = () => SchemaCreator.ValidatePrefix(prefix);
        act.Should().NotThrow();
    }

    [Theory]
    [InlineData("")]
    [InlineData(null)]
    [InlineData("g")]
    [InlineData("g0a")]
    [InlineData("g1; DROP TABLE")]
    [InlineData("'; EXEC sp_evil--")]
    [InlineData("[g1]")]
    [InlineData("g1_training")]
    [InlineData("G1")]
    [InlineData("gym1")]
    [InlineData("g1.")]
    [InlineData("g 1")]
    [InlineData(" g1")]
    public void ValidatePrefix_Rejects_AnythingNotMatchingNumericGPattern(string? prefix)
    {
        var act = () => SchemaCreator.ValidatePrefix(prefix!);
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void PerGymModuleSchemas_Includes_AllNineModules()
    {
        SchemaCreator.PerGymModuleSchemas.Should().BeEquivalentTo(new[]
        {
            "gyms", "training", "progress", "scheduling",
            "nutrition", "communication", "billing", "gamification", "outbox"
        });
    }
}
