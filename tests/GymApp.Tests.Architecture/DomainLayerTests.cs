using FluentAssertions;
using NetArchTest.Rules;
using System.Reflection;

namespace GymApp.Tests.Architecture;

public class DomainLayerTests : BaseTest
{
    public static IEnumerable<object[]> Modules => AllModules();

    [Theory]
    [MemberData(nameof(Modules))]
    public void Domain_Should_Not_Reference_Application(string module) =>
        AssertNoDependency(
            GetAssembly(module, "Domain"),
            $"GymApp.Modules.{module}.Application",
            $"{module}.Domain should not reference {module}.Application.");

    [Theory]
    [MemberData(nameof(Modules))]
    public void Domain_Should_Not_Reference_Infrastructure(string module) =>
        AssertNoDependency(
            GetAssembly(module, "Domain"),
            $"GymApp.Modules.{module}.Infrastructure",
            $"{module}.Domain should not reference {module}.Infrastructure.");

    [Theory]
    [MemberData(nameof(Modules))]
    public void Domain_Should_Not_Reference_Presentation(string module) =>
        AssertNoDependency(
            GetAssembly(module, "Domain"),
            $"GymApp.Modules.{module}.Presentation",
            $"{module}.Domain should not reference {module}.Presentation.");

    [Theory]
    [MemberData(nameof(Modules))]
    public void Domain_Should_Not_Reference_EntityFramework(string module) =>
        AssertNoDependency(
            GetAssembly(module, "Domain"),
            "Microsoft.EntityFrameworkCore",
            $"{module}.Domain should not reference Entity Framework Core.");

    [Theory]
    [MemberData(nameof(Modules))]
    public void Domain_Entities_Should_Inherit_From_Entity_Base(string module)
    {
        var domainAssembly = GetAssembly(module, "Domain");
        var entityBaseType = typeof(Common.Domain.Entity);

        var entityTypes = Types.InAssembly(domainAssembly)
            .That()
            .ResideInNamespaceContaining("Entities")
            .And()
            .AreClasses()
            .And()
            .AreNotAbstract()
            .GetTypes()
            .Where(t => !IsRecord(t));

        foreach (var type in entityTypes)
        {
            type.Should().BeDerivedFrom(entityBaseType,
                $"{type.FullName} in {module}.Domain should inherit from Entity base class");
        }
    }

    [Theory]
    [MemberData(nameof(Modules))]
    public void Domain_Should_Not_Reference_MediatR_Directly(string module)
    {
        // Domain should only reference IDomainEvent (via Common.Domain), not MediatR directly
        var domainAssembly = GetAssembly(module, "Domain");

        var domainTypes = Types.InAssembly(domainAssembly)
            .That()
            .DoNotResideInNamespaceContaining("Events")
            .GetTypes();

        foreach (var type in domainTypes)
        {
            var usesMediatR = type.GetInterfaces()
                .Any(i => i.Namespace?.StartsWith("MediatR") == true);

            usesMediatR.Should().BeFalse(
                $"{type.FullName} in {module}.Domain should not directly implement MediatR interfaces");
        }
    }

    private static bool IsRecord(Type type)
    {
        // Records have a compiler-generated <Clone>$ method
        return type.GetMethod("<Clone>$") is not null;
    }
}
