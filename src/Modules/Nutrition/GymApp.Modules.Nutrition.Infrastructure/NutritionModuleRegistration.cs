using FluentValidation;
using GymApp.Common.Application.Abstractions;
using GymApp.Common.Application.Behaviors;
using GymApp.Common.Infrastructure.Interceptors;
using GymApp.Common.Infrastructure.Tenancy;
using GymApp.Modules.Nutrition.Application.Abstractions;
using GymApp.Modules.Nutrition.Domain.Repositories;
using GymApp.Modules.Nutrition.Infrastructure.Persistence;
using GymApp.Modules.Nutrition.Infrastructure.Persistence.Seed;
using GymApp.Modules.Nutrition.Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace GymApp.Modules.Nutrition.Infrastructure;

public static class NutritionModuleRegistration
{
    public static IServiceCollection AddNutritionModule(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddDbContext<NutritionDbContext>((sp, options) =>
        {
            options.UseSqlServer(configuration.GetConnectionString("Database"))
                .UseTenantSchemaResolution()
                .AddInterceptors(sp.GetRequiredService<ConvertDomainEventsToOutboxMessagesInterceptor>());
        });

        services.AddScoped<INutritionUnitOfWork>(sp => sp.GetRequiredService<NutritionDbContext>());

        services.AddScoped<IFoodLogRepository, FoodLogRepository>();
        services.AddScoped<IWaterLogRepository, WaterLogRepository>();
        services.AddScoped<IFoodDatabaseRepository, FoodDatabaseRepository>();
        services.AddScoped<IMealPlanRepository, MealPlanRepository>();
        services.AddScoped<IUserDataProvider, GymApp.Modules.Nutrition.Application.Services.UserDataProvider>();

        services.AddMediatR(cfg =>
        {
            cfg.RegisterServicesFromAssembly(typeof(GymApp.Modules.Nutrition.Application.Abstractions.INutritionUnitOfWork).Assembly);
            cfg.AddOpenBehavior(typeof(ValidationBehavior<,>));
        });

        services.AddValidatorsFromAssembly(
            typeof(GymApp.Modules.Nutrition.Application.Abstractions.INutritionUnitOfWork).Assembly,
            includeInternalTypes: true);

        return services;
    }

    /// <summary>
    /// Seeds the food database with Egyptian food items if the table is empty.
    /// Call this during application startup.
    /// </summary>
    public static async Task SeedFoodDatabaseAsync(IServiceProvider serviceProvider)
    {
        var dbContext = serviceProvider.GetRequiredService<NutritionDbContext>();

        if (await dbContext.FoodDatabaseItems.AnyAsync())
            return;

        var foods = EgyptianFoodSeedData.GetAll();
        await dbContext.FoodDatabaseItems.AddRangeAsync(foods);
        await dbContext.SaveChangesAsync();
    }
}
