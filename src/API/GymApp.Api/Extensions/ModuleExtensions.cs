using GymApp.Modules.Identity.Infrastructure;
using GymApp.Modules.Gyms.Infrastructure;
using GymApp.Modules.Training.Infrastructure;
using GymApp.Modules.Progress.Infrastructure;
using GymApp.Modules.Scheduling.Infrastructure;
using GymApp.Modules.Nutrition.Infrastructure;
using GymApp.Modules.Communication.Infrastructure;
using GymApp.Modules.Billing.Infrastructure;
using GymApp.Modules.Gamification.Infrastructure;

namespace GymApp.Api.Extensions;

/// <summary>
/// Registers all nine feature modules, plus the reflection-based assembly
/// force-loading the outbox deserializer and the endpoint scanner depend on.
/// </summary>
public static class ModuleExtensions
{
    public static WebApplicationBuilder AddApplicationModules(this WebApplicationBuilder builder)
    {
        // --- Ensure all module assemblies are loaded for Outbox type resolution ---
        // The Newtonsoft.Json outbox processor uses TypeNameHandling.All and needs all domain event
        // assemblies in the AppDomain before it can deserialize events from other modules.
        _ = typeof(GymApp.Modules.Identity.Domain.Errors.UserErrors).Assembly;
        _ = typeof(GymApp.Modules.Gyms.Domain.Errors.GymErrors).Assembly;
        _ = typeof(GymApp.Modules.Training.Domain.Errors.TrainingErrors).Assembly;
        _ = typeof(GymApp.Modules.Progress.Domain.ProgressErrors).Assembly;
        _ = typeof(GymApp.Modules.Scheduling.Domain.Errors.SchedulingErrors).Assembly;
        _ = typeof(GymApp.Modules.Nutrition.Domain.Errors.NutritionErrors).Assembly;
        _ = typeof(GymApp.Modules.Communication.Domain.Errors.CommunicationErrors).Assembly;
        _ = typeof(GymApp.Modules.Billing.Domain.Errors.BillingErrors).Assembly;
        _ = typeof(GymApp.Modules.Gamification.Domain.Errors.GamificationErrors).Assembly;

        // --- Force-load Presentation assemblies so the endpoint scan can discover IEndpoint types.
        // Project references alone don't load assemblies until the runtime needs them; the internal sealed
        // Endpoint classes are never referenced by name from here, so we load the DLLs explicitly.
        foreach (var dll in Directory.GetFiles(AppContext.BaseDirectory, "GymApp.Modules.*.Presentation.dll"))
        {
            try { System.Reflection.Assembly.LoadFrom(dll); } catch { /* ignore */ }
        }

        // --- Module Registration (each module adds its own DbContext + services) ---
        builder.Services.AddIdentityModule(builder.Configuration);
        builder.Services.AddGymsModule(builder.Configuration);
        builder.Services.AddTrainingModule(builder.Configuration);
        builder.Services.AddProgressModule(builder.Configuration);
        builder.Services.AddSchedulingModule(builder.Configuration);
        builder.Services.AddNutritionModule(builder.Configuration);
        builder.Services.AddCommunicationModule(builder.Configuration);
        builder.Services.AddBillingModule(builder.Configuration);
        builder.Services.AddGamificationModule(builder.Configuration);

        return builder;
    }
}
