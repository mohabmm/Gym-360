using FluentAssertions;
using NetArchTest.Rules;
using GymApp.Common.Presentation;

namespace GymApp.Tests.Architecture;

public class PresentationLayerTests : BaseTest
{
    public static IEnumerable<object[]> Modules => AllModules();

    [Theory]
    [MemberData(nameof(Modules))]
    public void Endpoints_Should_Implement_IEndpoint(string module)
    {
        var presAssembly = GetAssembly(module, "Presentation");

        var endpointTypes = Types.InAssembly(presAssembly)
            .That()
            .HaveNameEndingWith("Endpoint")
            .And()
            .AreClasses()
            .GetTypes();

        foreach (var type in endpointTypes)
        {
            var implementsIEndpoint = type.GetInterfaces()
                .Any(i => i == typeof(IEndpoint));

            implementsIEndpoint.Should().BeTrue(
                $"Endpoint {type.FullName} should implement IEndpoint");
        }
    }

    [Theory]
    [MemberData(nameof(Modules))]
    public void Endpoints_Should_Be_Internal_Sealed(string module)
    {
        var presAssembly = GetAssembly(module, "Presentation");

        var endpointTypes = Types.InAssembly(presAssembly)
            .That()
            .HaveNameEndingWith("Endpoint")
            .And()
            .AreClasses()
            .GetTypes();

        foreach (var type in endpointTypes)
        {
            type.IsSealed.Should().BeTrue(
                $"Endpoint {type.FullName} should be sealed");
            type.IsPublic.Should().BeFalse(
                $"Endpoint {type.FullName} should be internal (not public)");
        }
    }

    [Theory]
    [MemberData(nameof(Modules))]
    public void Presentation_Should_Not_Reference_Infrastructure(string module)
    {
        var presAssembly = GetAssembly(module, "Presentation");
        var infraNamespace = $"GymApp.Modules.{module}.Infrastructure";

        var result = Types.InAssembly(presAssembly)
            .ShouldNot()
            .HaveDependencyOn(infraNamespace)
            .GetResult();

        result.IsSuccessful.Should().BeTrue(
            $"{module}.Presentation should not reference {module}.Infrastructure. " +
            $"Failing types: {FormatFailingTypes(result.FailingTypeNames)}");
    }

    private static string FormatFailingTypes(IEnumerable<string>? typeNames)
    {
        if (typeNames is null) return "none";
        return string.Join(", ", typeNames);
    }
}
