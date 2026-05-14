using FluentAssertions;
using NetArchTest.Rules;
using System.Reflection;

namespace GymApp.Tests.Architecture;

public class ApplicationLayerTests : BaseTest
{
    public static IEnumerable<object[]> Modules => AllModules();

    [Theory]
    [MemberData(nameof(Modules))]
    public void Application_Should_Not_Reference_Infrastructure(string module)
    {
        var appAssembly = GetAssembly(module, "Application");
        var infraNamespace = $"GymApp.Modules.{module}.Infrastructure";

        var result = Types.InAssembly(appAssembly)
            .ShouldNot()
            .HaveDependencyOn(infraNamespace)
            .GetResult();

        result.IsSuccessful.Should().BeTrue(
            $"{module}.Application should not reference {module}.Infrastructure. " +
            $"Failing types: {FormatFailingTypes(result.FailingTypeNames)}");
    }

    [Theory]
    [MemberData(nameof(Modules))]
    public void Application_Should_Not_Reference_Presentation(string module)
    {
        var appAssembly = GetAssembly(module, "Application");
        var presentationNamespace = $"GymApp.Modules.{module}.Presentation";

        var result = Types.InAssembly(appAssembly)
            .ShouldNot()
            .HaveDependencyOn(presentationNamespace)
            .GetResult();

        result.IsSuccessful.Should().BeTrue(
            $"{module}.Application should not reference {module}.Presentation. " +
            $"Failing types: {FormatFailingTypes(result.FailingTypeNames)}");
    }

    [Theory]
    [MemberData(nameof(Modules))]
    public void Command_Handlers_Should_Be_Internal_Sealed(string module)
    {
        var appAssembly = GetAssembly(module, "Application");

        var handlerTypes = Types.InAssembly(appAssembly)
            .That()
            .ImplementInterface(typeof(Common.Application.Messaging.ICommandHandler<>))
            .Or()
            .ImplementInterface(typeof(Common.Application.Messaging.ICommandHandler<,>))
            .GetTypes();

        foreach (var type in handlerTypes)
        {
            type.IsSealed.Should().BeTrue(
                $"Command handler {type.FullName} should be sealed");
            type.IsPublic.Should().BeFalse(
                $"Command handler {type.FullName} should be internal (not public)");
        }
    }

    [Theory]
    [MemberData(nameof(Modules))]
    public void Query_Handlers_Should_Be_Internal_Sealed(string module)
    {
        var appAssembly = GetAssembly(module, "Application");

        var handlerTypes = Types.InAssembly(appAssembly)
            .That()
            .HaveNameEndingWith("QueryHandler")
            .GetTypes();

        foreach (var type in handlerTypes)
        {
            type.IsSealed.Should().BeTrue(
                $"Query handler {type.FullName} should be sealed");
            type.IsPublic.Should().BeFalse(
                $"Query handler {type.FullName} should be internal (not public)");
        }
    }

    [Theory]
    [MemberData(nameof(Modules))]
    public void Validators_Should_Be_Internal_Sealed(string module)
    {
        var appAssembly = GetAssembly(module, "Application");

        var validatorTypes = Types.InAssembly(appAssembly)
            .That()
            .HaveNameEndingWith("Validator")
            .GetTypes();

        foreach (var type in validatorTypes)
        {
            type.IsSealed.Should().BeTrue(
                $"Validator {type.FullName} should be sealed");
            type.IsPublic.Should().BeFalse(
                $"Validator {type.FullName} should be internal (not public)");
        }
    }

    [Theory]
    [MemberData(nameof(Modules))]
    public void Application_Should_Not_Reference_EntityFramework(string module)
    {
        var appAssembly = GetAssembly(module, "Application");

        var result = Types.InAssembly(appAssembly)
            .ShouldNot()
            .HaveDependencyOn("Microsoft.EntityFrameworkCore")
            .GetResult();

        result.IsSuccessful.Should().BeTrue(
            $"{module}.Application should not reference Entity Framework Core. " +
            $"Failing types: {FormatFailingTypes(result.FailingTypeNames)}");
    }

    private static string FormatFailingTypes(IEnumerable<string>? typeNames)
    {
        if (typeNames is null) return "none";
        return string.Join(", ", typeNames);
    }
}
