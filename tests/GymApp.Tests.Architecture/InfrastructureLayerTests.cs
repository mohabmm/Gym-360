using FluentAssertions;
using NetArchTest.Rules;
using GymApp.Common.Domain;

namespace GymApp.Tests.Architecture;

public class InfrastructureLayerTests : BaseTest
{
    public static IEnumerable<object[]> Modules => AllModules();

    [Theory]
    [MemberData(nameof(Modules))]
    public void Infrastructure_Should_Not_Reference_Presentation(string module)
    {
        var infraAssembly = GetAssembly(module, "Infrastructure");
        var presentationNamespace = $"GymApp.Modules.{module}.Presentation";

        var result = Types.InAssembly(infraAssembly)
            .ShouldNot()
            .HaveDependencyOn(presentationNamespace)
            .GetResult();

        result.IsSuccessful.Should().BeTrue(
            $"{module}.Infrastructure should not reference {module}.Presentation. " +
            $"Failing types: {FormatFailingTypes(result.FailingTypeNames)}");
    }

    [Theory]
    [MemberData(nameof(Modules))]
    public void DbContexts_Should_Implement_IUnitOfWork(string module)
    {
        var infraAssembly = GetAssembly(module, "Infrastructure");

        var dbContextTypes = Types.InAssembly(infraAssembly)
            .That()
            .HaveNameEndingWith("DbContext")
            .And()
            .AreClasses()
            .GetTypes();

        dbContextTypes.Should().NotBeEmpty(
            $"{module}.Infrastructure should have at least one DbContext");

        foreach (var type in dbContextTypes)
        {
            var implementsUoW = type.GetInterfaces()
                .Any(i => i == typeof(IUnitOfWork));

            implementsUoW.Should().BeTrue(
                $"DbContext {type.FullName} should implement IUnitOfWork");
        }
    }

    [Theory]
    [MemberData(nameof(Modules))]
    public void Infrastructure_Should_Not_Use_Dapper(string module)
    {
        var infraAssembly = GetAssembly(module, "Infrastructure");

        var result = Types.InAssembly(infraAssembly)
            .ShouldNot()
            .HaveDependencyOn("Dapper")
            .GetResult();

        result.IsSuccessful.Should().BeTrue(
            $"{module}.Infrastructure must use pure EF Core -- no Dapper allowed. " +
            $"Failing types: {FormatFailingTypes(result.FailingTypeNames)}");
    }

    private static string FormatFailingTypes(IEnumerable<string>? typeNames)
    {
        if (typeNames is null) return "none";
        return string.Join(", ", typeNames);
    }
}
