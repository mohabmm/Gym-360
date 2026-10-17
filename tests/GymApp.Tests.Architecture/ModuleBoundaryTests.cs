namespace GymApp.Tests.Architecture;

public class ModuleBoundaryTests : BaseTest
{
    /// <summary>
    /// Layers that must NOT cross-reference other modules (only IntegrationEvents is allowed).
    /// </summary>
    private static readonly string[] ProtectedLayers = { "Domain", "Application", "Infrastructure" };

    public static IEnumerable<object[]> ModulePairsWithLayers() =>
        from sourceModule in ModuleNames
        from targetModule in OtherModules(sourceModule)
        from sourceLayer in ProtectedLayers
        from targetLayer in ProtectedLayers
        select new object[] { sourceModule, sourceLayer, targetModule, targetLayer };

    [Theory]
    [MemberData(nameof(ModulePairsWithLayers))]
    public void Module_Should_Not_Reference_Another_Modules_Protected_Layers(
        string sourceModule, string sourceLayer, string targetModule, string targetLayer) =>
        AssertNoDependency(
            GetAssembly(sourceModule, sourceLayer),
            $"GymApp.Modules.{targetModule}.{targetLayer}",
            $"{sourceModule}.{sourceLayer} must not reference {targetModule}.{targetLayer}. " +
            $"Only IntegrationEvents cross-references are allowed.");

    [Theory]
    [MemberData(nameof(Modules))]
    public void Presentation_Should_Not_Reference_Other_Modules_Protected_Layers(string module)
    {
        var presAssembly = GetAssembly(module, "Presentation");

        var forbiddenTargets =
            from otherModule in OtherModules(module)
            from layer in ProtectedLayers
            select $"{otherModule}.{layer}";

        foreach (var target in forbiddenTargets)
        {
            AssertNoDependency(
                presAssembly,
                $"GymApp.Modules.{target}",
                $"{module}.Presentation must not reference {target}.");
        }
    }

    public static IEnumerable<object[]> Modules => AllModules();

    private static IEnumerable<string> OtherModules(string module) =>
        ModuleNames.Where(other => other != module);
}
