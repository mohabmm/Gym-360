using FluentAssertions;
using NetArchTest.Rules;

namespace GymApp.Tests.Architecture;

/// <summary>
/// Guards the per-module <c>.Shared</c> projects — the shared sync-contract surface.
/// A module's shared surface is exactly <c>.Shared</c> (sync contracts) +
/// <c>.IntegrationEvents</c> (async events); everything else is module-internal.
/// <c>.Shared</c> projects must stay leaf nodes so the contract surface cannot
/// erode back into cross-module coupling.
/// </summary>
public class ModuleContractProjectTests : BaseTest
{
    /// <summary>
    /// Modules that currently expose a cross-module sync contract via a <c>.Shared</c> project.
    /// </summary>
    private static readonly string[] ContractModules = { "Gyms", "Identity", "Billing", "Gamification" };

    private static readonly string[] InternalLayers = { "Domain", "Application", "Infrastructure" };

    public static IEnumerable<object[]> ContractModulesData => ContractModules.Select(m => new object[] { m });

    [Theory]
    [MemberData(nameof(ContractModulesData))]
    public void Contract_Project_Should_Not_Depend_On_Any_Module_Internal_Layer(string contractModule)
    {
        var contractAssembly = GetAssembly(contractModule, "Shared");

        foreach (var module in ModuleNames)
        {
            foreach (var layer in InternalLayers)
            {
                var targetNamespace = $"GymApp.Modules.{module}.{layer}";

                var result = Types.InAssembly(contractAssembly)
                    .ShouldNot()
                    .HaveDependencyOn(targetNamespace)
                    .GetResult();

                result.IsSuccessful.Should().BeTrue(
                    $"{contractModule}.Shared is a leaf contract project and must not reference {targetNamespace}. " +
                    $"Shared surface = .Shared + .IntegrationEvents; all internal layers stay module-private. " +
                    $"Failing types: {string.Join(", ", result.FailingTypeNames ?? Enumerable.Empty<string>())}");
            }
        }
    }
}
