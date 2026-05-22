using FluentValidation;
using GymApp.Common.Application.Abstractions;
using GymApp.Modules.Gyms.Shared;
using GymApp.Common.Application.Behaviors;
using GymApp.Common.Infrastructure.Interceptors;
using GymApp.Common.Infrastructure.Tenancy;
using GymApp.Modules.Gyms.Application.Abstractions;
using GymApp.Modules.Gyms.Application.Contracts;
using GymApp.Modules.Gyms.Domain.Repositories;
using GymApp.Modules.Gyms.Infrastructure.Data;
using GymApp.Modules.Gyms.Infrastructure.Jobs;
using GymApp.Modules.Gyms.Infrastructure.Repositories;
using GymApp.Modules.Gyms.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Quartz;

namespace GymApp.Modules.Gyms.Infrastructure;

public static class GymsModuleRegistration
{
    public static IServiceCollection AddGymsModule(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddDbContext<GymsDbContext>((sp, options) =>
        {
            options.UseSqlServer(configuration.GetConnectionString("Database"))
                .UseTenantSchemaResolution();
            options.AddInterceptors(sp.GetRequiredService<ConvertDomainEventsToOutboxMessagesInterceptor>());
        });

        services.AddScoped<IGymsUnitOfWork>(sp => sp.GetRequiredService<GymsDbContext>());

        services.AddScoped<IGymRepository, GymRepository>();
        services.AddScoped<IGymClassRepository, GymClassRepository>();
        services.AddScoped<IAnnouncementRepository, AnnouncementRepository>();
        services.AddScoped<ICheckInRepository, CheckInRepository>();
        services.AddScoped<IStaffRoleRepository, StaffRoleRepository>();
        services.AddScoped<IGymSessionPolicyRepository, GymSessionPolicyRepository>();
        services.AddScoped<IGymSectionRepository, GymSectionRepository>();
        services.AddScoped<IGymSeasonalHoursRepository, GymSeasonalHoursRepository>();
        services.AddScoped<ITrainerRateRepository, TrainerRateRepository>();
        services.AddScoped<IMemberNoteRepository, MemberNoteRepository>();
        services.AddScoped<IMemberOnboardingRepository, MemberOnboardingRepository>();
        services.AddScoped<IDataImportJobRepository, DataImportJobRepository>();
        services.AddScoped<IGymTrainerRequestRepository, GymTrainerRequestRepository>();
        services.AddScoped<IGymMemberRepository, GymMemberRepository>();
        services.AddSingleton<IImportFileStorage, LocalImportFileStorage>();
        services.AddScoped<IImportParser, ExcelImportParser>();
        services.AddScoped<IImportJobScheduler, QuartzImportJobScheduler>();
        services.AddScoped<IImportTemplateProvider, ExcelImportTemplateProvider>();

        services.AddScoped<IGymQueryService, GymQueryService>();
        services.AddScoped<IGymLookupService, GymLookupService>();
        services.AddScoped<IGymMembershipChecker, GymMembershipChecker>();
        services.AddScoped<IGymMemberDirectory, GymMemberDirectory>();
        services.AddScoped<IUserDataProvider, GymApp.Modules.Gyms.Application.Services.UserDataProvider>();
        services.AddScoped<IStaffSubRoleResolver, StaffSubRoleResolver>();
        services.AddSingleton<IKioskPinHasher, KioskPinHasher>();

        services.AddMediatR(cfg =>
        {
            cfg.RegisterServicesFromAssembly(typeof(GymApp.Modules.Gyms.Application.Contracts.IGymQueryService).Assembly);
            cfg.AddOpenBehavior(typeof(ValidationBehavior<,>));
        });

        services.AddValidatorsFromAssembly(
            typeof(GymApp.Modules.Gyms.Application.Contracts.IGymQueryService).Assembly,
            includeInternalTypes: true);

        // Background job: auto-transition gym subscription states every 6 hours
        services.AddQuartz(q =>
        {
            var jobKey = new JobKey(nameof(ProcessGymSubscriptionStatusJob));
            q.AddJob<ProcessGymSubscriptionStatusJob>(opts => opts.WithIdentity(jobKey));
            q.AddTrigger(opts => opts
                .ForJob(jobKey)
                .WithIdentity($"{nameof(ProcessGymSubscriptionStatusJob)}-trigger")
                .WithSimpleSchedule(s => s.WithIntervalInHours(6).RepeatForever()));
        });

        return services;
    }
}
