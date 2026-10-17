using NetArchTest.Rules;

namespace GymApp.Tests.Architecture;

public class ApplicationLayerTests : BaseTest
{
    public static IEnumerable<object[]> Modules => AllModules();

    [Theory]
    [MemberData(nameof(Modules))]
    public void Application_Should_Not_Reference_Infrastructure(string module) =>
        AssertNoDependency(
            GetAssembly(module, "Application"),
            $"GymApp.Modules.{module}.Infrastructure",
            $"{module}.Application should not reference {module}.Infrastructure.");

    [Theory]
    [MemberData(nameof(Modules))]
    public void Application_Should_Not_Reference_Presentation(string module) =>
        AssertNoDependency(
            GetAssembly(module, "Application"),
            $"GymApp.Modules.{module}.Presentation",
            $"{module}.Application should not reference {module}.Presentation.");

    [Theory]
    [MemberData(nameof(Modules))]
    public void Command_Handlers_Should_Be_Internal_Sealed(string module)
    {
        var handlerTypes = Types.InAssembly(GetAssembly(module, "Application"))
            .That()
            .ImplementInterface(typeof(Common.Application.Messaging.ICommandHandler<>))
            .Or()
            .ImplementInterface(typeof(Common.Application.Messaging.ICommandHandler<,>))
            .GetTypes();

        AssertInternalSealed(handlerTypes, "Command handler");
    }

    [Theory]
    [MemberData(nameof(Modules))]
    public void Query_Handlers_Should_Be_Internal_Sealed(string module) =>
        AssertInternalSealed(TypesNamedLike(module, "QueryHandler"), "Query handler");

    [Theory]
    [MemberData(nameof(Modules))]
    public void Validators_Should_Be_Internal_Sealed(string module) =>
        AssertInternalSealed(TypesNamedLike(module, "Validator"), "Validator");

    [Theory]
    [MemberData(nameof(Modules))]
    public void Application_Should_Not_Reference_EntityFramework(string module) =>
        AssertNoDependency(
            GetAssembly(module, "Application"),
            "Microsoft.EntityFrameworkCore",
            $"{module}.Application should not reference Entity Framework Core.");

    private static IEnumerable<Type> TypesNamedLike(string module, string nameSuffix) =>
        Types.InAssembly(GetAssembly(module, "Application"))
            .That()
            .HaveNameEndingWith(nameSuffix)
            .GetTypes();
}
