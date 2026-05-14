using FluentAssertions;
using NetArchTest.Rules;
using GymApp.Common.Domain;

namespace GymApp.Tests.Architecture;

public class DomainEventTests : BaseTest
{
    public static IEnumerable<object[]> Modules => AllModules();

    [Theory]
    [MemberData(nameof(Modules))]
    public void Domain_Events_Should_Be_Sealed(string module)
    {
        var domainAssembly = GetAssembly(module, "Domain");

        var domainEventTypes = Types.InAssembly(domainAssembly)
            .That()
            .ImplementInterface(typeof(IDomainEvent))
            .GetTypes();

        foreach (var type in domainEventTypes)
        {
            type.IsSealed.Should().BeTrue(
                $"Domain event {type.FullName} should be sealed");
        }
    }

    [Theory]
    [MemberData(nameof(Modules))]
    public void Domain_Events_Should_End_With_DomainEvent(string module)
    {
        var domainAssembly = GetAssembly(module, "Domain");

        var domainEventTypes = Types.InAssembly(domainAssembly)
            .That()
            .ImplementInterface(typeof(IDomainEvent))
            .GetTypes();

        foreach (var type in domainEventTypes)
        {
            type.Name.Should().EndWith("DomainEvent",
                $"Domain event {type.FullName} should have a name ending with 'DomainEvent'");
        }
    }

    [Theory]
    [MemberData(nameof(Modules))]
    public void Domain_Events_Should_Reside_In_Events_Namespace(string module)
    {
        var domainAssembly = GetAssembly(module, "Domain");

        var domainEventTypes = Types.InAssembly(domainAssembly)
            .That()
            .ImplementInterface(typeof(IDomainEvent))
            .GetTypes();

        foreach (var type in domainEventTypes)
        {
            type.Namespace.Should().Contain("Events",
                $"Domain event {type.FullName} should reside in an Events namespace");
        }
    }

    [Fact]
    public void All_Modules_Should_Have_At_Least_One_Domain_Event()
    {
        foreach (var module in ModuleNames)
        {
            var domainAssembly = GetAssembly(module, "Domain");

            var domainEventTypes = Types.InAssembly(domainAssembly)
                .That()
                .ImplementInterface(typeof(IDomainEvent))
                .GetTypes();

            domainEventTypes.Should().NotBeEmpty(
                $"{module}.Domain should have at least one domain event");
        }
    }
}
