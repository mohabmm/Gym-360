using System.Reflection;

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
}
