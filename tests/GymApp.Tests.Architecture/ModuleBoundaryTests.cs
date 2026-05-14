using FluentAssertions;
using NetArchTest.Rules;

namespace GymApp.Tests.Architecture;

public class ModuleBoundaryTests : BaseTest
{
    /// <summary>
    /// Layers that must NOT cross-reference other modules (only IntegrationEvents is allowed).
    /// </summary>
    private static readonly string[] ProtectedLayers = { "Domain", "Application", "Infrastructure" };

    public static IEnumerable<object[]> ModulePairsWithLayers()
    {
        foreach (var sourceModule in ModuleNames)
        {
            foreach (var targetModule in ModuleNames)
            {
                if (sourceModule == targetModule) continue;

                foreach (var sourceLayer in ProtectedLayers)
                {
                    foreach (var targetLayer in ProtectedLayers)
                    {
                        yield return new object[] { sourceModule, sourceLayer, targetModule, targetLayer };
                    }
                }
            }
        }
    }

    [Theory]
    [MemberData(nameof(ModulePairsWithLayers))]
    public void Module_Should_Not_Reference_Another_Modules_Protected_Layers(
        string sourceModule, string sourceLayer, string targetModule, string targetLayer)
    {
        var sourceAssembly = GetAssembly(sourceModule, sourceLayer);
        var targetNamespace = $"GymApp.Modules.{targetModule}.{targetLayer}";

        var result = Types.InAssembly(sourceAssembly)
            .ShouldNot()
            .HaveDependencyOn(targetNamespace)
            .GetResult();

        result.IsSuccessful.Should().BeTrue(
            $"{sourceModule}.{sourceLayer} must not reference {targetModule}.{targetLayer}. " +
            $"Only IntegrationEvents cross-references are allowed. " +
            $"Failing types: {FormatFailingTypes(result.FailingTypeNames)}");
    }

    [Theory]
    [MemberData(nameof(Modules))]
    public void Presentation_Should_Not_Reference_Other_Modules_Protected_Layers(string module)
    {
        var presAssembly = GetAssembly(module, "Presentation");

        foreach (var otherModule in ModuleNames)
        {
            if (otherModule == module) continue;

            foreach (var layer in ProtectedLayers)
            {
                var targetNamespace = $"GymApp.Modules.{otherModule}.{layer}";

                var result = Types.InAssembly(presAssembly)
                    .ShouldNot()
                    .HaveDependencyOn(targetNamespace)
                    .GetResult();

                result.IsSuccessful.Should().BeTrue(
                    $"{module}.Presentation must not reference {otherModule}.{layer}. " +
                    $"Failing types: {FormatFailingTypes(result.FailingTypeNames)}");
            }
        }
    }

    public static IEnumerable<object[]> Modules => AllModules();

    private static string FormatFailingTypes(IEnumerable<string>? typeNames)
    {
        if (typeNames is null) return "none";
        return string.Join(", ", typeNames);
    }
}
