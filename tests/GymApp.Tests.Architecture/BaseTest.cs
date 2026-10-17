using System.Reflection;
using FluentAssertions;
using NetArchTest.Rules;

namespace GymApp.Tests.Architecture;

public abstract class BaseTest
{
    protected static readonly string[] ModuleNames =
    {
        "Identity",
        "Gyms",
        "Training",
        "Progress",
        "Scheduling",
        "Nutrition",
        "Communication",
        "Billing",
        "Gamification"
    };

    protected static readonly string[] LayerNames =
    {
        "Domain",
        "Application",
        "Infrastructure",
        "Presentation",
        "IntegrationEvents"
    };

    protected static Assembly GetAssembly(string module, string layer)
    {
        var assemblyName = $"GymApp.Modules.{module}.{layer}";
        return Assembly.Load(assemblyName);
    }

    protected static Assembly GetCommonAssembly(string layer)
    {
        var assemblyName = $"GymApp.Common.{layer}";
        return Assembly.Load(assemblyName);
    }

    protected static IEnumerable<object[]> AllModules()
    {
        return ModuleNames.Select(m => new object[] { m });
    }

    /// <summary>
    /// Asserts no type in <paramref name="assembly"/> depends on <paramref name="forbiddenNamespace"/>.
    /// <paramref name="reason"/> is the failure message; the failing types are appended to it.
    /// </summary>
    protected static void AssertNoDependency(Assembly assembly, string forbiddenNamespace, string reason)
    {
        var result = Types.InAssembly(assembly)
            .ShouldNot()
            .HaveDependencyOn(forbiddenNamespace)
            .GetResult();

        result.IsSuccessful.Should().BeTrue(
            $"{reason} Failing types: {FormatFailingTypes(result.FailingTypeNames)}");
    }

    /// <summary>
    /// Asserts every type is sealed and internal; <paramref name="kind"/> names the type
    /// kind in failure messages (e.g. "Command handler").
    /// </summary>
    protected static void AssertInternalSealed(IEnumerable<Type> types, string kind)
    {
        foreach (var type in types)
        {
            type.IsSealed.Should().BeTrue(
                $"{kind} {type.FullName} should be sealed");
            type.IsPublic.Should().BeFalse(
                $"{kind} {type.FullName} should be internal (not public)");
        }
    }

    private static string FormatFailingTypes(IEnumerable<string>? typeNames)
    {
        if (typeNames is null) return "none";
        return string.Join(", ", typeNames);
    }
}
